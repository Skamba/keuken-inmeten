"""Playwright test for: wanddikte, gaatjesrij, and wandopstelling features."""
from playwright.sync_api import sync_playwright
import os, sys

BASE = "http://localhost:5181"
OUT = os.path.join(os.path.dirname(__file__), "screenshots")
os.makedirs(OUT, exist_ok=True)
PASS = 0
FAIL = 0

def screenshot(page, name, wait_ms=500):
    page.wait_for_timeout(wait_ms)
    path = os.path.join(OUT, f"{name}.png")
    page.screenshot(path=path, full_page=True)
    print(f"  📸 {name}.png")

def blazor_fill(page, selector, value):
    loc = page.locator(selector)
    loc.click()
    loc.fill(str(value))
    loc.dispatch_event("change")
    page.wait_for_timeout(200)

def blazor_select(page, selector, value=None, index=None):
    loc = page.locator(selector)
    if index is not None:
        loc.select_option(index=index)
    else:
        loc.select_option(value)
    loc.dispatch_event("change")
    page.wait_for_timeout(200)

def check(label, condition):
    global PASS, FAIL
    if condition:
        PASS += 1
        print(f"  ✅ {label}")
    else:
        FAIL += 1
        print(f"  ❌ {label}")

with sync_playwright() as p:
    browser = p.chromium.launch()
    page = browser.new_page(viewport={"width": 1280, "height": 900})

    errors = []
    page.on("console", lambda msg: errors.append(msg.text) if msg.type == "error" else None)
    page.on("pageerror", lambda err: errors.append(str(err)))

    # Load app
    print("Loading Blazor WASM...")
    page.goto(f"{BASE}/", timeout=120000)
    try:
        page.locator(".navbar-brand").wait_for(state="visible", timeout=60000)
        print("  Blazor loaded!\n")
    except Exception:
        print(f"  FAILED to load. Errors: {errors}")
        browser.close()
        sys.exit(1)

    # Navigate to kasten
    page.goto(f"{BASE}/kasten", timeout=30000)
    page.locator("text=Keuken indeling").wait_for(state="visible", timeout=30000)

    # Add a wall
    print("1. Wand toevoegen")
    blazor_fill(page, 'input[placeholder*="Linkerwand"]', "Keuken achterwand")
    page.click("button:has-text('Wand toevoegen')")
    page.wait_for_timeout(500)

    # ── Test Onderkast with new fields ──
    print("\n2. Onderkast toevoegen met wanddikte + gaatjesrij")
    page.click("button:has-text('Kast toevoegen')")
    page.wait_for_timeout(500)

    # Verify default values for new fields
    card = page.locator(".card.bg-light")
    wanddikte_input = card.locator("input[type='number']").nth(3)
    hoogte_vloer_input = card.locator("input[type='number']").nth(4)
    gaatjes_afstand_input = card.locator("input[type='number']").nth(5)
    eerste_gat_input = card.locator("input[type='number']").nth(6)

    check("Wanddikte default = 18", wanddikte_input.input_value() == "18")
    check("HoogteVanVloer default = 0", hoogte_vloer_input.input_value() == "0")
    check("GaatjesAfstand default = 32", gaatjes_afstand_input.input_value() == "32")
    check("EersteGaatVanBoven default = 37", eerste_gat_input.input_value() == "37")

    # Fill cabinet name
    blazor_fill(page, 'input[placeholder*="Onderkast spoelbak"]', "Onderkast links")

    # Set custom wanddikte
    blazor_fill(page, ".card.bg-light input[type='number'] >> nth=3", "16")
    check("Wanddikte set to 16", card.locator("input[type='number']").nth(3).input_value() == "16")

    # Set custom gaatjesrij
    blazor_fill(page, ".card.bg-light input[type='number'] >> nth=5", "32")
    blazor_fill(page, ".card.bg-light input[type='number'] >> nth=6", "37")

    # Calculate standard positions
    page.click("button:has-text('Standaard berekenen')")
    page.wait_for_timeout(300)

    screenshot(page, "test_01_onderkast_form")

    # Add the cabinet
    page.locator("button.btn-primary:has-text('Kast toevoegen')").click()
    page.wait_for_timeout(500)

    # ── Test Bovenkast with type change ──
    print("\n3. Bovenkast toevoegen — Type change sets HoogteVanVloer")
    blazor_fill(page, 'input[placeholder*="Onderkast spoelbak"]', "Bovenkast midden")

    # Change type to Bovenkast — should auto-set HoogteVanVloer to 1400
    blazor_select(page, ".card.bg-light select.form-select >> nth=0", "Bovenkast")
    page.wait_for_timeout(300)

    hoogte_vloer_val = card.locator("input[type='number']").nth(4).input_value()
    check(f"Type→Bovenkast auto-sets HoogteVanVloer=1400 (got {hoogte_vloer_val})", hoogte_vloer_val == "1400")

    # Set bovenkast hoogte
    blazor_fill(page, ".card.bg-light input[type='number'] >> nth=1", "360")
    page.click("button:has-text('Standaard berekenen')")
    page.wait_for_timeout(300)

    screenshot(page, "test_02_bovenkast_form")

    page.locator("button.btn-primary:has-text('Kast toevoegen')").click()
    page.wait_for_timeout(500)

    # ── Test HogeKast ──
    print("\n4. Hoge kast toevoegen")
    blazor_fill(page, 'input[placeholder*="Onderkast spoelbak"]', "Hoge kast rechts")
    blazor_select(page, ".card.bg-light select.form-select >> nth=0", "HogeKast")
    page.wait_for_timeout(300)

    hoogte_vloer_val = card.locator("input[type='number']").nth(4).input_value()
    check(f"Type→HogeKast sets HoogteVanVloer=0 (got {hoogte_vloer_val})", hoogte_vloer_val == "0")

    blazor_fill(page, ".card.bg-light input[type='number'] >> nth=1", "2100")
    page.click("button:has-text('Standaard berekenen')")
    page.wait_for_timeout(300)

    page.locator("button.btn-primary:has-text('Kast toevoegen')").click()
    page.wait_for_timeout(500)

    # Close form to see the full view
    page.click("button:has-text('Annuleren')")
    page.wait_for_timeout(300)

    # ── Test WandOpstelling visual ──
    print("\n5. WandOpstelling visualisatie")
    check("WandOpstelling SVG visible", page.locator("h6:has-text('Wandopstelling')").is_visible())
    wand_svg = page.locator("h6:has-text('Wandopstelling') + div svg")
    check("WandOpstelling SVG element exists", wand_svg.count() > 0)

    screenshot(page, "test_03_wandopstelling")

    # ── Test KastVisueel with gaatjesrij ──
    print("\n6. KastVisueel met gaatjesrij")
    # Click on onderkast edit button in chip list
    page.locator("button[title='Bewerken']").first.click()
    page.wait_for_timeout(500)

    # The KastVisueel should be visible in the form with hole row circles
    kast_svg = page.locator(".card.bg-light svg")
    check("KastVisueel SVG visible in form", kast_svg.count() > 0)

    screenshot(page, "test_04_kastvisueel_gaatjes")

    # ── Scroll to see full page with WandOpstelling ──
    print("\n7. Volledige pagina overzicht")
    page.click("button:has-text('Annuleren')")
    page.wait_for_timeout(300)
    screenshot(page, "test_05_volledig_overzicht")

    # ── Navigate to resultaat to verify data flows through ──
    print("\n8. Panelen + Resultaat flow check")
    page.click("text=Volgende stap: Panelen configureren")
    page.locator("text=Panelen configureren").first.wait_for(state="visible", timeout=15000)
    page.wait_for_timeout(500)

    # Add a panel for onderkast
    kast_select = page.locator("select.form-select").first
    kast_select.select_option(index=1)
    kast_select.dispatch_event("change")
    page.wait_for_timeout(500)
    page.click("button:has-text('Paneel toevoegen')")
    page.wait_for_timeout(500)

    # Navigate to resultaat
    page.click("text=Volgende stap: Resultaat bekijken")
    page.locator("text=Resultaat").first.wait_for(state="visible", timeout=15000)
    page.wait_for_timeout(500)

    check("Resultaat page loads with data", page.locator("td:has-text('Onderkast links')").is_visible())
    screenshot(page, "test_06_resultaat")

    browser.close()

    # Summary
    print(f"\n{'='*40}")
    print(f"Results: {PASS} passed, {FAIL} failed")
    if errors:
        print(f"Browser errors: {errors}")
    print(f"{'='*40}")

    if FAIL > 0:
        sys.exit(1)
