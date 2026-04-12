export function printCurrentPage() {
    window.print();
}

function downloadBlob(filename, blob) {
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    setTimeout(() => URL.revokeObjectURL(url), 1000);
}

export function downloadTextFile(filename, content, mimeType) {
    const blob = new Blob(["\uFEFF", content], { type: mimeType });
    downloadBlob(filename, blob);
}

function getPdfMake() {
    if (!window.pdfMake || typeof window.pdfMake.createPdf !== "function") {
        throw new Error("pdfMake is not available.");
    }

    return window.pdfMake;
}

function getValue(source, camelName, pascalName) {
    const resolvedPascalName = pascalName ?? `${camelName[0].toUpperCase()}${camelName.slice(1)}`;
    return source?.[camelName] ?? source?.[resolvedPascalName];
}

function normalizeBoorGatPayload(boorGat) {
    return {
        nummer: getValue(boorGat, "nummer"),
        xCncLabel: getValue(boorGat, "xCncLabel", "XCncLabel"),
        yCncLabel: getValue(boorGat, "yCncLabel", "YCncLabel"),
    };
}

function normalizeRegelPayload(regel) {
    return {
        regelNummer: getValue(regel, "regelNummer"),
        regelCode: getValue(regel, "regelCode"),
        naam: getValue(regel, "naam"),
        aantal: getValue(regel, "aantal"),
        paneelMeta: getValue(regel, "paneelMeta"),
        bronLocaties: getValue(regel, "bronLocaties") ?? [],
        zaagmaatLabel: getValue(regel, "zaagmaatLabel"),
        oppervlaktePerStukLabel: getValue(regel, "oppervlaktePerStukLabel"),
        totaleOppervlakteLabel: getValue(regel, "totaleOppervlakteLabel"),
        materiaalLabel: getValue(regel, "materiaalLabel"),
        boorbeeldSamenvatting: getValue(regel, "boorbeeldSamenvatting"),
        cncReferentieLabel: getValue(regel, "cncReferentieLabel"),
        geenBoorwerkTekst: getValue(regel, "geenBoorwerkTekst"),
        boorgaten: (getValue(regel, "boorgaten") ?? []).map(normalizeBoorGatPayload),
        visualSvg: getValue(regel, "visualSvg"),
    };
}

function normalizePdfPayload(payload) {
    return {
        titel: getValue(payload, "titel"),
        generatedAtLabel: getValue(payload, "generatedAtLabel"),
        paneelType: getValue(payload, "paneelType"),
        dikteLabel: getValue(payload, "dikteLabel"),
        cncReferentieLabel: getValue(payload, "cncReferentieLabel"),
        totaalOppervlakteLabel: getValue(payload, "totaalOppervlakteLabel"),
        orderregels: getValue(payload, "orderregels"),
        totaalAantal: getValue(payload, "totaalAantal"),
        totaalBoorgaten: getValue(payload, "totaalBoorgaten"),
        regels: (getValue(payload, "regels") ?? []).map(normalizeRegelPayload),
    };
}

function buildMetaCard(label, value, style) {
    return {
        stack: [
            { text: label, style: `${style}Label` },
            { text: value, style: `${style}Value` },
        ],
        fillColor: style === "summary" ? "#ffffff" : "#f8fafc",
        margin: [8, 6, 8, 6],
    };
}

function buildBoorbeeldStack(regel) {
    if (!regel.boorgaten.length) {
        return [{ text: regel.geenBoorwerkTekst, style: "mutedText" }];
    }

    return [
        { text: regel.boorbeeldSamenvatting, style: "drillSummary" },
        { text: regel.cncReferentieLabel, style: "drillReference", margin: [0, 2, 0, 4] },
        ...regel.boorgaten.map((boorgat) => ({
            text: `#${boorgat.nummer} · X ${boorgat.xCncLabel} · Y ${boorgat.yCncLabel}`,
            style: "drillCoordinate",
        })),
    ];
}

function buildPdfRows(payload) {
    const body = [
        [
            {
                text: `Materiaal ${payload.paneelType} · ${payload.dikteLabel} · ${payload.cncReferentieLabel} · ${payload.totaalOppervlakteLabel} totaal oppervlak`,
                colSpan: 6,
                style: "repeatMeta",
            },
            {},
            {},
            {},
            {},
            {},
        ],
        [
            { text: "Regel", style: "tableHeader" },
            { text: "Paneel en bronlocaties", style: "tableHeader" },
            { text: "Aantal", style: "tableHeader", alignment: "center" },
            { text: "Eindmaat na kantenband", style: "tableHeader" },
            { text: "Boorbeeld / CNC", style: "tableHeader" },
            { text: "Visual", style: "tableHeader", alignment: "center" },
        ],
    ];

    for (const regel of payload.regels) {
        body.push([
            {
                stack: [
                    { text: regel.regelCode, style: "ruleCode", alignment: "center" },
                    { text: `Regel ${regel.regelNummer}`, style: "mutedText", alignment: "center" },
                ],
            },
            {
                stack: [
                    { text: regel.naam, style: "panelName" },
                    { text: regel.paneelMeta, style: "panelMeta", margin: [0, 2, 0, 4] },
                    { text: "Bronlocaties", style: "contextTitle", margin: [0, 2, 0, 2] },
                    { ul: regel.bronLocaties, style: "locationsList" },
                ],
            },
            {
                text: String(regel.aantal),
                style: "qtyValue",
                alignment: "center",
                margin: [0, 18, 0, 0],
            },
            {
                stack: [
                    { text: regel.zaagmaatLabel, style: "cutSize" },
                    { text: `${regel.oppervlaktePerStukLabel} per stuk`, style: "detailText" },
                    { text: `${regel.totaleOppervlakteLabel} totaal`, style: "detailText" },
                    { text: regel.materiaalLabel, style: "detailText" },
                ],
            },
            {
                stack: buildBoorbeeldStack(regel),
            },
            {
                svg: regel.visualSvg,
                fit: [125, 145],
                alignment: "center",
                margin: [0, 2, 0, 2],
            },
        ]);
    }

    return body;
}

function createBestellijstPdfDefinition(payload) {
    return {
        pageSize: "A4",
        pageOrientation: "landscape",
        pageMargins: [24, 24, 24, 26],
        footer: (currentPage, pageCount) => ({
            text: `${currentPage} / ${pageCount}`,
            alignment: "right",
            margin: [0, 0, 24, 12],
            fontSize: 8,
            color: "#64748b",
        }),
        defaultStyle: {
            fontSize: 9,
            color: "#1f2937",
        },
        content: [
            {
                columns: [
                    {
                        width: "*",
                        text: payload.titel,
                        style: "title",
                    },
                    {
                        width: 160,
                        stack: [
                            { text: "Gegenereerd", style: "generatedLabel", alignment: "right" },
                            { text: payload.generatedAtLabel, style: "generatedValue", alignment: "right" },
                        ],
                    },
                ],
                columnGap: 12,
                margin: [0, 0, 0, 10],
            },
            {
                table: {
                    widths: ["*", "*", "*", "*"],
                    body: [[
                        buildMetaCard("Materiaal", payload.paneelType, "meta"),
                        buildMetaCard("Dikte", payload.dikteLabel, "meta"),
                        buildMetaCard("CNC referentie", payload.cncReferentieLabel, "meta"),
                        buildMetaCard("Totaal oppervlak", payload.totaalOppervlakteLabel, "meta"),
                    ]],
                },
                layout: {
                    hLineColor: () => "#d8e1ea",
                    vLineColor: () => "#d8e1ea",
                },
                margin: [0, 0, 0, 10],
            },
            {
                table: {
                    widths: ["*", "*", "*", "*"],
                    body: [[
                        buildMetaCard("Orderregels", String(payload.orderregels), "summary"),
                        buildMetaCard("Totaal panelen", String(payload.totaalAantal), "summary"),
                        buildMetaCard("Totaal 35 mm potscharniergaten", String(payload.totaalBoorgaten), "summary"),
                        buildMetaCard("Totaal oppervlak", payload.totaalOppervlakteLabel, "summary"),
                    ]],
                },
                layout: {
                    hLineColor: () => "#d8e1ea",
                    vLineColor: () => "#d8e1ea",
                },
                margin: [0, 0, 0, 10],
            },
            {
                table: {
                    headerRows: 2,
                    dontBreakRows: true,
                    widths: [58, 192, 44, 116, 172, 142],
                    body: buildPdfRows(payload),
                },
                layout: {
                    hLineColor: () => "#d9e1ea",
                    vLineColor: () => "#d9e1ea",
                    paddingLeft: () => 6,
                    paddingRight: () => 6,
                    paddingTop: () => 5,
                    paddingBottom: () => 5,
                },
            },
        ],
        styles: {
            title: {
                fontSize: 18,
                bold: true,
                color: "#0f172a",
            },
            generatedLabel: {
                fontSize: 7.5,
                bold: true,
                color: "#64748b",
            },
            generatedValue: {
                fontSize: 10,
                bold: true,
                color: "#0f172a",
            },
            metaLabel: {
                fontSize: 7,
                bold: true,
                color: "#64748b",
            },
            metaValue: {
                fontSize: 10,
                bold: true,
                color: "#0f172a",
            },
            summaryLabel: {
                fontSize: 7,
                bold: true,
                color: "#64748b",
            },
            summaryValue: {
                fontSize: 15,
                bold: true,
                color: "#0f4c81",
            },
            repeatMeta: {
                fontSize: 7.6,
                color: "#475569",
                fillColor: "#f8fafc",
                bold: true,
            },
            tableHeader: {
                fontSize: 7.6,
                bold: true,
                color: "#0f172a",
                fillColor: "#edf4fb",
            },
            ruleCode: {
                fontSize: 13,
                bold: true,
                color: "#0f4c81",
            },
            panelName: {
                fontSize: 10,
                bold: true,
                color: "#0f172a",
            },
            panelMeta: {
                fontSize: 8,
                color: "#334155",
            },
            contextTitle: {
                fontSize: 7.2,
                bold: true,
                color: "#64748b",
            },
            locationsList: {
                fontSize: 7.6,
                color: "#5b6470",
                margin: [8, 0, 0, 0],
            },
            qtyValue: {
                fontSize: 15,
                bold: true,
                color: "#0f172a",
            },
            cutSize: {
                fontSize: 10,
                bold: true,
                color: "#0f172a",
            },
            detailText: {
                fontSize: 7.8,
                color: "#5b6470",
            },
            drillSummary: {
                fontSize: 8.2,
                bold: true,
                color: "#0f172a",
            },
            drillReference: {
                fontSize: 7.2,
                color: "#64748b",
            },
            drillCoordinate: {
                fontSize: 7.4,
                color: "#334155",
            },
            mutedText: {
                fontSize: 7.6,
                color: "#64748b",
            },
        },
    };
}

export async function downloadPdfDocument(filename, payloadJson) {
    try {
        const payload = normalizePdfPayload(JSON.parse(payloadJson));
        const pdfDocument = getPdfMake().createPdf(createBestellijstPdfDefinition(payload));
        const blob = await new Promise((resolve, reject) => {
            try {
                pdfDocument.getBlob(resolve);
            } catch (error) {
                reject(error);
            }
        });

        downloadBlob(filename, blob);
    } catch (error) {
        const message = error instanceof Error ? error.message : String(error);
        throw new Error(`PDF download failed: ${message}`);
    }
}

export async function shareUrl(url, title, text) {
    if (navigator.share && (!navigator.canShare || navigator.canShare({ url }))) {
        try {
            await navigator.share({ url, title, text });
            return "shared";
        } catch (error) {
            if (error && error.name === "AbortError") {
                return "cancelled";
            }
        }
    }

    try {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            await navigator.clipboard.writeText(url);
            return "copied";
        }
    } catch {
        // Fall back to execCommand below.
    }

    const input = document.createElement("textarea");
    input.value = url;
    input.setAttribute("readonly", "");
    input.style.position = "fixed";
    input.style.opacity = "0";
    document.body.appendChild(input);
    input.select();
    input.setSelectionRange(0, input.value.length);
    const copied = document.execCommand("copy");
    document.body.removeChild(input);
    return copied ? "copied" : "unsupported";
}
