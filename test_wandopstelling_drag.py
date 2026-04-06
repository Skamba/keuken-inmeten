"""Playwright test for: WandOpstelling drag & drop and wall dimension inputs."""
from playwright.sync_api import sync_playwright
import os

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

    # ─── Load app ───
    print("\n🔧 Navigating to app...")
    page.goto(BASE, wait_until="networkidle")
    page.wait_for_timeout(2000)

    # ─── Navigate to Kasten page ───
    print("\n📐 Navigate to Kasten page")
    page.click("a[href='kasten']")
    page.wait_for_timeout(1000)

    # ─── Add a wall ───
    print("\n🧱 Adding a wall")
    blazor_fill(page, "input[placeholder*='Linkerwand']", "Testwand")
    page.click("button:has-text('Wand toevoegen')")
    page.wait_for_timeout(500)

    # ─── Test 1: Wall dimension inputs exist ───
    print("\n📏 Test wall dimension inputs")
    breedte_label = page.locator("text=Wandbreedte (mm)")
    hoogte_label = page.locator("text=Wandhoogte (mm)")
    check("Wandbreedte label visible", breedte_label.count() > 0)
    check("Wandhoogte label visible", hoogte_label.count() > 0)

    # Find inputs by their parent label structure
    breedte_input = breedte_label.locator("..").locator("input")
    hoogte_input = hoogte_label.locator("..").locator("input")
    check("Wandbreedte input visible", breedte_input.count() > 0 and breedte_input.first.is_visible())
    check("Wandhoogte input visible", hoogte_input.count() > 0 and hoogte_input.first.is_visible())

    # ─── Test 2: Change wall dimensions ───
    print("\n📏 Test changing wall dimensions")
    blazor_fill(page, "label:has-text('Wandbreedte') >> xpath=.. >> input", "4000")
    page.wait_for_timeout(500)
    check("Wandbreedte changed to 4000", breedte_input.first.input_value() == "4000")

    blazor_fill(page, "label:has-text('Wandhoogte') >> xpath=.. >> input", "3000")
    page.wait_for_timeout(500)
    check("Wandhoogte changed to 3000", hoogte_input.first.input_value() == "3000")

    screenshot(page, "drag01_wall_dimensions")

    # ─── Add a cabinet to trigger WandOpstelling ───
    print("\n🗄️ Adding cabinets")
    page.click("button:has-text('+ Kast toevoegen')")
    page.wait_for_timeout(300)

    blazor_fill(page, "input[placeholder*='Onderkast spoelbak']", "Kast A")
    page.wait_for_timeout(200)

    page.click("button:has-text('Kast toevoegen'):not(:has-text('+'))")
    page.wait_for_timeout(500)

    # ─── Test 3: WandOpstelling appears with cabinet ───
    print("\n🏗️ Test WandOpstelling rendering")
    svg = page.locator("svg.wand-opstelling-svg")
    check("WandOpstelling SVG visible", svg.count() > 0 and svg.first.is_visible())

    kast_group = page.locator("g.wand-kast-sleepbaar")
    check("Cabinet group in SVG", kast_group.count() > 0)

    screenshot(page, "drag02_wandopstelling_one_cabinet")

    # ─── Add a second cabinet (form stays open after first save) ───
    blazor_fill(page, "input[placeholder*='Onderkast spoelbak']", "Kast B")
    page.wait_for_timeout(200)
    page.click("button:has-text('Kast toevoegen'):not(:has-text('+'))")
    page.wait_for_timeout(500)

    check("Two cabinets in WandOpstelling", page.locator("g.wand-kast-sleepbaar").count() == 2)

    screenshot(page, "drag03_wandopstelling_two_cabinets")

    # ─── Test 4: Drag a cabinet (simulate pointer events) ───
    print("\n🖱️ Test drag operation")
    svg_el = svg.first
    svg_box = svg_el.bounding_box()

    first_kast = kast_group.first
    kast_box = first_kast.bounding_box()

    if kast_box:
        start_x = kast_box["x"] + kast_box["width"] / 2
        start_y = kast_box["y"] + kast_box["height"] / 2
        end_x = start_x + 80
        end_y = start_y - 40

        page.mouse.move(start_x, start_y)
        page.mouse.down()
        page.wait_for_timeout(100)
        page.mouse.move(end_x, end_y, steps=10)
        page.wait_for_timeout(100)
        page.mouse.up()
        page.wait_for_timeout(500)

        screenshot(page, "drag04_after_drag")
        check("Drag completed without errors", len([e for e in errors if "Error" in e or "error" in e.lower()]) == 0)
    else:
        check("Could find cabinet bounding box", False)

    # ─── Test 5: Click cabinet to select ───
    print("\n👆 Test cabinet selection via click")
    second_kast = kast_group.nth(1)
    second_kast.click()
    page.wait_for_timeout(500)
    screenshot(page, "drag05_cabinet_selected")

    # Check that edit form shows
    edit_form = page.locator("text=Kast bewerken")
    check("Clicking cabinet opens edit form", edit_form.count() > 0)

    # ─── Test 6: Wall labels in SVG ───
    print("\n📐 Test wall dimension labels in SVG")
    svg_text = svg.first.inner_html()
    check("SVG shows wall width label", "4000" in svg_text)
    check("SVG shows wall height label", "3000" in svg_text)

    # ─── Final screenshot ───
    screenshot(page, "drag06_final_state")

    # ─── Summary ───
    print(f"\n{'='*40}")
    print(f"Results: {PASS} passed, {FAIL} failed out of {PASS+FAIL}")
    if FAIL > 0:
        print("SOME TESTS FAILED")
    else:
        print("ALL TESTS PASSED ✅")
    print(f"{'='*40}\n")

    if errors:
        print("Console errors captured:")
        for e in errors[:10]:
            print(f"  ⚠️ {e}")

    browser.close()
