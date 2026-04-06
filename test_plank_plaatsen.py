"""Playwright test: place a shelf (plank) inside a cabinet via double-click."""
import time
import os
from playwright.sync_api import sync_playwright

BASE_URL = "http://localhost:5182"
SCREENSHOT_DIR = os.path.join(os.path.dirname(__file__), "screenshots_plank")
os.makedirs(SCREENSHOT_DIR, exist_ok=True)

step = 0
def ss(page, name):
    global step
    step += 1
    p = os.path.join(SCREENSHOT_DIR, f"{step:02d}_{name}.png")
    page.screenshot(path=p, full_page=True)
    print(f"  [{step}] {p}")

def blazor(page, sel, val):
    loc = page.locator(sel)
    loc.fill(val)
    loc.dispatch_event("change")
    time.sleep(0.3)

def blazor_loc(loc, val):
    loc.fill(val)
    loc.dispatch_event("change")
    time.sleep(0.3)

def wait(page):
    page.wait_for_load_state("networkidle")
    time.sleep(0.5)

def main():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=False, slow_mo=100)
        ctx = browser.new_context(viewport={"width": 1400, "height": 900})
        page = ctx.new_page()
        console_msgs = []
        page.on("console", lambda msg: console_msgs.append(f"[{msg.type}] {msg.text}") if "[WO]" in msg.text else None)
        # ── 1. Load ──
        print("1. Load app...")
        page.goto(BASE_URL + "/kasten")
        wait(page)
        ss(page, "start")

        # ── 2. Add a wall ──
        print("2. Add wall 'muur'...")
        blazor(page, "input[placeholder*='Linkerwand']", "muur")
        page.click("button:has-text('Wand toevoegen')")
        wait(page)
        ss(page, "wand_added")

        # ── 3. Open kast form ──
        print("3. Open kast form...")
        page.locator("button:has-text('+ Kast toevoegen')").first.click()
        wait(page)
        ss(page, "kast_form_open")

        form = page.locator(".card.border-success")
        naam_input = form.locator("input[placeholder*='Onderkast']")
        naam_input.fill("Testkast")
        naam_input.dispatch_event("change")
        time.sleep(0.3)

        # Fill number inputs one by one with explicit labels to avoid nth fragility
        breedte = form.locator("label:has-text('Breedte')").locator("~ div input, + input").first
        # Simpler: use label text to find the input in the same col-md div
        number_inputs = form.locator("input[type='number']")
        for i, val in enumerate(["600", "1900", "560", "18"]):
            inp = number_inputs.nth(i)
            inp.fill(val)
            inp.dispatch_event("change")
            time.sleep(0.3)
        ss(page, "kast_filled")

        # ── 4. Save cabinet ──
        print("4. Save cabinet...")
        form.locator("button:has-text('Kast toevoegen')").click()
        wait(page)
        ss(page, "kast_saved")

        # ── 5. Locate the SVG and the cabinet group inside it ──
        print("5. Locate SVG and cabinet...")
        svg = page.locator("svg.wand-opstelling-svg").first
        svg.wait_for(state="visible", timeout=5000)
        ss(page, "svg_visible")

        # Get the bounding box of the cabinet group
        kast_group = page.locator("g.wand-kast-sleepbaar").first
        kast_box = kast_group.bounding_box()
        print(f"   Cabinet bounding box: {kast_box}")

        if kast_box is None:
            print("ERROR: Could not find cabinet group in SVG!")
            ss(page, "ERROR_no_kast")
            browser.close()
            return

        # ── 6. Double-click in the UPPER third of the cabinet (avoid sticky nav bar at bottom) ──
        cx = kast_box["x"] + kast_box["width"] / 2
        cy = kast_box["y"] + kast_box["height"] * 0.25  # upper quarter, well away from bottom nav
        print(f"   Double-clicking at ({cx:.0f}, {cy:.0f}) (upper quarter of cabinet)...")
        page.mouse.dblclick(cx, cy)
        time.sleep(1.5)
        ss(page, "after_dblclick")

        print("   Console logs:")
        for m in console_msgs: print("  ", m)

        # ── 7. Check if a plank was added ──
        plank_count = page.locator("g.wand-plank-sleepbaar").count()
        print(f"   Planks found after dblclick: {plank_count}")

        if plank_count == 0:
            print("FAIL: No plank added by double-click!")
            # Try to get console errors
            ss(page, "FAIL_no_plank")
        else:
            print(f"PASS: {plank_count} plank(s) added!")
            ss(page, "PASS_plank_added")

        # ── 8. Try Delete key to remove the plank ──
        if plank_count > 0:
            print("8. Test Delete key removes plank...")
            svg.focus()
            page.keyboard.press("Delete")
            wait(page)
            plank_count_after = page.locator("g.wand-plank-sleepbaar").count()
            print(f"   Planks after Delete: {plank_count_after}")
            ss(page, "after_delete")
            if plank_count_after == 0:
                print("PASS: Plank removed by Delete key!")
            else:
                print("FAIL: Plank NOT removed by Delete key!")

        browser.close()
        print("Done.")

if __name__ == "__main__":
    main()
