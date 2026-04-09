const vendorScriptUrl = new URL('./vendor/fflate.min.js', import.meta.url);
let fflatePromise;

function ensureFflate() {
  if (globalThis.fflate) {
    return Promise.resolve(globalThis.fflate);
  }

  if (!fflatePromise) {
    fflatePromise = new Promise((resolve, reject) => {
      const resolveLoadedLibrary = () => {
        if (globalThis.fflate) {
          resolve(globalThis.fflate);
          return;
        }

        reject(new Error('De share-compressiebibliotheek kon niet worden geladen.'));
      };

      const rejectLoad = () => reject(new Error('De share-compressiebibliotheek kon niet worden geladen.'));
      const bestaandScript = document.querySelector('script[data-keuken-share-fflate="true"]');

      if (bestaandScript) {
        bestaandScript.addEventListener('load', resolveLoadedLibrary, { once: true });
        bestaandScript.addEventListener('error', rejectLoad, { once: true });
        return;
      }

      const script = document.createElement('script');
      script.async = true;
      script.src = vendorScriptUrl.href;
      script.dataset.keukenShareFflate = 'true';
      script.addEventListener('load', resolveLoadedLibrary, { once: true });
      script.addEventListener('error', rejectLoad, { once: true });
      document.head.append(script);
    }).catch((error) => {
      document.querySelector('script[data-keuken-share-fflate="true"]')?.remove();
      fflatePromise = undefined;
      throw error;
    });
  }

  return fflatePromise;
}

function bytesToBase64(bytes) {
  let binary = '';
  const chunkSize = 0x8000;

  for (let index = 0; index < bytes.length; index += chunkSize) {
    binary += String.fromCharCode(...bytes.subarray(index, index + chunkSize));
  }

  return btoa(binary);
}

function base64ToBytes(base64) {
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);

  for (let index = 0; index < binary.length; index += 1) {
    bytes[index] = binary.charCodeAt(index);
  }

  return bytes;
}

function toBase64Url(bytes) {
  return bytesToBase64(bytes)
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/g, '');
}

function fromBase64Url(base64Url) {
  const padded = base64Url
    .replace(/-/g, '+')
    .replace(/_/g, '/');
  const remainder = padded.length % 4;
  const normalized = remainder === 0
    ? padded
    : padded.padEnd(padded.length + (4 - remainder), '=');

  return base64ToBytes(normalized);
}

export async function compressSharePayload(json) {
  const fflate = await ensureFflate();
  const compressed = fflate.deflateSync(fflate.strToU8(json), { level: 6 });
  return toBase64Url(compressed);
}

export async function decompressSharePayload(compressedPayload) {
  const fflate = await ensureFflate();
  const decompressed = fflate.inflateSync(fromBase64Url(compressedPayload));
  return fflate.strFromU8(decompressed);
}
