"""Take screenshots of all pages in the Keuken Inmeten app, including states with data."""
from playwright.sync_api import sync_playwright
import os

BASE = "http://localhost:5181"
OUT = os.path.join(os.path.dirname(__file__), "screenshots")
os.makedirs(OUT, exist_ok=True)

def screenshot(page, name, wait_ms=500):
    page.wait_for_timeout(wait_ms)
    path = os.path.join(OUT, f"{name}.png")
    page.screenshot(path=path, full_page=True)
    print(f"  -> {name}.png")

def blazor_fill(page, selector, value):
    """Fill and trigger change event for Blazor @bind."""
    loc = page.locator(selector)
    loc.click()
    loc.fill(value)
    loc.dispatch_event("change")
    page.wait_for_timeout(200)

def blazor_select(page, selector, value=None, index=None):
    """Select option and trigger change event for Blazor."""
    loc = page.locator(selector)
    if index is not None:
        loc.select_option(index=index)
    else:
        loc.select_option(value)
    loc.dispatch_event("change")
    page.wait_for_timeout(200)

def goto(page, path, wait_selector=None, timeout=30000):
    page.goto(f"{BASE}{path}", timeout=timeout)
    if wait_selector:
        page.locator(wait_selector).first.wait_for(state="visible", timeout=timeout)
    page.wait_for_timeout(500)

with sync_playwright() as p:
    browser = p.chromium.launch()
    page = browser.new_page(viewport={"width": 1280, "height": 900})

    # Capture errors
    errors = []
    page.on("console", lambda msg: errors.append(msg.text) if msg.type == "error" else None)
    page.on("pageerror", lambda err: errors.append(str(err)))

    # Load app and wait for Blazor WASM to initialize
    print("Loading Blazor WASM...")
    page.goto(f"{BASE}/", timeout=120000)
    try:
        page.locator(".navbar-brand").wait_for(state="visible", timeout=60000)
        print("  Blazor loaded!")
    except Exception:
        print(f"  Timed out. Errors: {errors}")
        page.screenshot(path=os.path.join(OUT, "debug_failed.png"), full_page=True)
        browser.close()
        exit(1)

    # 1. Home
    print("1. Home")
    screenshot(page, "01_home")

    # 2. Kasten (empty)
    print("2. Kasten (empty)")
    goto(page, "/kasten", "text=Keuken indeling")
    screenshot(page, "02_kasten_leeg")

    # 3. Add wall
    print("3. Add wall")
    blazor_fill(page, 'input[placeholder*="Linkerwand"]', "Achterwand")
    page.click("button:has-text('Wand toevoegen')")
    page.wait_for_timeout(500)
    screenshot(page, "03_wand_toegevoegd")

    # 4. Open cabinet form
    print("4. Cabinet form")
    page.click("button:has-text('Kast toevoegen')")
    page.wait_for_timeout(500)
    screenshot(page, "04_kast_formulier")

    # 5. Fill cabinet
    print("5. Fill & add cabinet")
    blazor_fill(page, 'input[placeholder*="Onderkast spoelbak"]', "Onderkast links")
    page.click("button:has-text('Standaard berekenen')")
    page.wait_for_timeout(300)
    screenshot(page, "05_kast_ingevuld")
    page.locator("button.btn-primary:has-text('Kast toevoegen')").click()
    page.wait_for_timeout(500)
    screenshot(page, "06_eerste_kast")

    # 6. Second cabinet (bovenkast) - form stays open after first add
    print("6. Second cabinet")
    blazor_fill(page, 'input[placeholder*="Onderkast spoelbak"]', "Bovenkast midden")
    blazor_select(page, ".card.bg-light select.form-select >> nth=0", "Bovenkast")
    blazor_fill(page, ".card.bg-light input[type='number'] >> nth=1", "360")
    page.click("button:has-text('Standaard berekenen')")
    page.wait_for_timeout(300)
    page.locator("button.btn-primary:has-text('Kast toevoegen')").click()
    page.wait_for_timeout(500)

    # 7. Third cabinet (hoge kast) - form still open
    print("7. Third cabinet")
    blazor_fill(page, 'input[placeholder*="Onderkast spoelbak"]', "Hoge kast rechts")
    blazor_select(page, ".card.bg-light select.form-select >> nth=0", "HogeKast")
    blazor_fill(page, ".card.bg-light input[type='number'] >> nth=1", "2100")
    page.click("button:has-text('Standaard berekenen')")
    page.wait_for_timeout(300)
    page.locator("button.btn-primary:has-text('Kast toevoegen')").click()
    page.wait_for_timeout(500)
    # Close the form first
    page.click("button:has-text('Annuleren')")
    page.wait_for_timeout(300)
    screenshot(page, "07_drie_kasten")

    # 8. Navigate to Panelen via client-side link (preserves WASM state)
    print("8. Panelen")
    page.click("text=Volgende stap: Panelen configureren")
    page.locator("text=Panelen configureren").first.wait_for(state="visible", timeout=15000)
    page.wait_for_timeout(500)
    screenshot(page, "08_panelen")

    # 9. Add panels
    print("9. Add panels")
    # Select first kast - use the onchange handler directly
    kast_select = page.locator("select.form-select").first
    kast_select.select_option(index=1)
    kast_select.dispatch_event("change")
    page.wait_for_timeout(500)
    screenshot(page, "09_paneel_formulier")

    page.click("button:has-text('Paneel toevoegen')")
    page.wait_for_timeout(500)

    # Select second kast 
    kast_select.select_option(index=2)
    kast_select.dispatch_event("change")
    page.wait_for_timeout(300)
    # Change type to LadeFront
    type_select = page.locator("select.form-select").nth(1)
    type_select.select_option("LadeFront")
    type_select.dispatch_event("change")
    page.wait_for_timeout(200)
    page.wait_for_timeout(200)
    page.click("button:has-text('Paneel toevoegen')")
    page.wait_for_timeout(300)
    screenshot(page, "10_panelen_klaar")

    # 10. Navigate to Resultaat via client-side link
    print("10. Resultaat")
    page.click("text=Volgende stap: Resultaat bekijken")
    page.locator("text=Resultaat").first.wait_for(state="visible", timeout=15000)
    page.wait_for_timeout(500)
    screenshot(page, "11_resultaat")

    page.evaluate("window.scrollTo(0, document.body.scrollHeight)")
    page.wait_for_timeout(300)
    screenshot(page, "12_resultaat_detail")

    browser.close()
    if errors:
        print(f"\nBrowser errors: {errors}")
    print("\nDone!")
