"""Comprehensive Playwright test for Keuken Inmeten app.

Creates multiple cabinets, navigates all steps, takes screenshots at every step.
"""

import os
import time
from playwright.sync_api import sync_playwright

BASE_URL = "http://localhost:5181"
SCREENSHOT_DIR = os.path.join(os.path.dirname(__file__), "screenshots")
os.makedirs(SCREENSHOT_DIR, exist_ok=True)

step_counter = 0


def screenshot(page, name):
    global step_counter
    step_counter += 1
    path = os.path.join(SCREENSHOT_DIR, f"{step_counter:02d}_{name}.png")
    page.screenshot(path=path, full_page=True)
    print(f"  Screenshot: {path}")


def blazor_fill(page, selector, value):
    """Fill input and trigger Blazor change event."""
    loc = page.locator(selector)
    loc.fill(value)
    loc.dispatch_event("change")
    time.sleep(0.3)


def blazor_fill_loc(locator, value):
    """Fill a locator and trigger Blazor change event."""
    locator.fill(value)
    locator.dispatch_event("change")
    time.sleep(0.3)


def wait_for_blazor(page):
    """Wait for Blazor WASM to fully load."""
    page.wait_for_load_state("networkidle")
    time.sleep(0.5)


def main():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context(viewport={"width": 1400, "height": 900})
        page = context.new_page()

        # ─── Load app ───
        print("1. Loading app...")
        page.goto(BASE_URL)
        wait_for_blazor(page)
        screenshot(page, "homepage")

        # ─── Navigate to Kasten page ───
        print("2. Navigating to Kasten page...")
        page.click("a[href='/kasten']")
        page.wait_for_url("**/kasten")
        wait_for_blazor(page)
        screenshot(page, "kasten_empty")

        # ─── Add first wand ───
        print("3. Adding first wand (Achterwand)...")
        blazor_fill(page, "input[placeholder*='Linkerwand']", "Achterwand")
        page.click("button:has-text('Wand toevoegen')")
        wait_for_blazor(page)
        screenshot(page, "wand_achterwand")

        # ─── Add second wand ───
        print("4. Adding second wand (Linkerwand)...")
        blazor_fill(page, "input[placeholder*='Linkerwand']", "Linkerwand")
        page.click("button:has-text('Wand toevoegen')")
        wait_for_blazor(page)
        screenshot(page, "wand_linkerwand")

        # ─── Add first cabinet to Achterwand ───
        print("5. Adding Onderkast spoelbak to Achterwand...")
        kast_buttons = page.locator("button:has-text('+ Kast toevoegen')")
        kast_buttons.first.click()
        wait_for_blazor(page)
        screenshot(page, "kast_form_open")

        # The standalone form is .card.border-success
        form = page.locator(".card.border-success")
        blazor_fill_loc(form.locator("input[placeholder*='Onderkast spoelbak']"), "Onderkast spoelbak")

        # Breedte (1st number input after Wand/Naam/Type row)
        number_inputs = form.locator("input[type='number']")
        blazor_fill_loc(number_inputs.nth(0), "600")  # Breedte
        blazor_fill_loc(number_inputs.nth(1), "720")  # Hoogte
        blazor_fill_loc(number_inputs.nth(2), "560")  # Diepte
        screenshot(page, "kast_filled")

        # Auto-calculate positions
        page.click("button:has-text('Standaard berekenen')")
        wait_for_blazor(page)
        screenshot(page, "kast_auto_positions")

        # Save
        form.locator("button:has-text('Kast toevoegen'):not(:has-text('+'))").click()
        wait_for_blazor(page)
        screenshot(page, "first_kast_saved")

        # ─── Add second cabinet (Onderkast vaatwasser) ───
        print("6. Adding Onderkast vaatwasser...")
        kast_buttons = page.locator("button:has-text('+ Kast toevoegen')")
        kast_buttons.first.click()
        wait_for_blazor(page)

        form = page.locator(".card.border-success")
        blazor_fill_loc(form.locator("input[placeholder*='Onderkast spoelbak']"), "Onderkast vaatwasser")
        number_inputs = form.locator("input[type='number']")
        blazor_fill_loc(number_inputs.nth(0), "450")  # Breedte smaller
        blazor_fill_loc(number_inputs.nth(1), "720")  # Hoogte
        blazor_fill_loc(number_inputs.nth(2), "560")  # Diepte

        page.click("button:has-text('Standaard berekenen')")
        wait_for_blazor(page)
        form.locator("button:has-text('Kast toevoegen'):not(:has-text('+'))").click()
        wait_for_blazor(page)
        screenshot(page, "second_kast_saved")

        # ─── Copy the first cabinet ───
        print("6b. Copying first cabinet...")
        copy_buttons = page.locator("button[title='Kopiëren']")
        if copy_buttons.count() > 0:
            copy_buttons.first.click()
            wait_for_blazor(page)
            screenshot(page, "kast_copied")

        # ─── Add third cabinet (Bovenkast) to Achterwand ───
        print("7. Adding Bovenkast boven spoelbak...")
        kast_buttons = page.locator("button:has-text('+ Kast toevoegen')")
        kast_buttons.first.click()
        wait_for_blazor(page)

        form = page.locator(".card.border-success")
        blazor_fill_loc(form.locator("input[placeholder*='Onderkast spoelbak']"), "Bovenkast boven spoelbak")

        number_inputs = form.locator("input[type='number']")
        blazor_fill_loc(number_inputs.nth(0), "600")  # Breedte
        blazor_fill_loc(number_inputs.nth(1), "600")  # Hoogte
        blazor_fill_loc(number_inputs.nth(2), "320")  # Diepte

        page.click("button:has-text('Standaard berekenen')")
        wait_for_blazor(page)
        form.locator("button:has-text('Kast toevoegen'):not(:has-text('+'))").click()
        wait_for_blazor(page)
        screenshot(page, "three_kasten")

        # ─── Add Hoge kast to Linkerwand ───
        print("8. Adding Hoge kast to Linkerwand...")
        kast_buttons = page.locator("button:has-text('+ Kast toevoegen')")
        kast_buttons.last.click()  # Last wand's button
        wait_for_blazor(page)

        form = page.locator(".card.border-success")
        blazor_fill_loc(form.locator("input[placeholder*='Onderkast spoelbak']"), "Hoge kast voorraad")

        number_inputs = form.locator("input[type='number']")
        blazor_fill_loc(number_inputs.nth(0), "600")   # Breedte
        blazor_fill_loc(number_inputs.nth(1), "2100")  # Hoogte
        blazor_fill_loc(number_inputs.nth(2), "560")   # Diepte

        page.click("button:has-text('Standaard berekenen')")
        wait_for_blazor(page)
        form.locator("button:has-text('Kast toevoegen'):not(:has-text('+'))").click()
        wait_for_blazor(page)
        screenshot(page, "hoge_kast_linkerwand")

        # ─── Edit a cabinet form ───
        print("9. Testing edit form...")
        edit_buttons = page.locator("button[title='Bewerken']")
        if edit_buttons.count() > 0:
            edit_buttons.first.click()
            wait_for_blazor(page)
            screenshot(page, "edit_kast")

            form = page.locator(".card.border-success")
            form.locator("button:has-text('Opslaan')").click()
            wait_for_blazor(page)

        # ─── Test drag & drop reorder ───
        print("10. Testing drag & drop...")
        # Compact cabinet list items are clickable/draggable
        wand_cards = page.locator(".card.border-primary")
        if wand_cards.count() > 0:
            first_wand = wand_cards.first
            compact_items = first_wand.locator(".d-flex.align-items-center.gap-1.border")
            if compact_items.count() >= 2:
                box1 = compact_items.nth(0).bounding_box()
                box2 = compact_items.nth(1).bounding_box()
                if box1 and box2:
                    page.mouse.move(box1["x"] + box1["width"]/2, box1["y"] + box1["height"]/2)
                    page.mouse.down()
                    page.mouse.move(box2["x"] + box2["width"]/2, box2["y"] + box2["height"]/2, steps=10)
                    page.mouse.up()
                    wait_for_blazor(page)
        screenshot(page, "after_reorder_attempt")

        # ─── Full kasten overview ───
        print("11. Full kasten overview...")
        screenshot(page, "kasten_overview")

        # ─── Navigate to Panelen ───
        print("12. Navigating to Panelen...")
        page.click("a:has-text('Volgende stap')")
        page.wait_for_url("**/panelen")
        wait_for_blazor(page)
        screenshot(page, "panelen_page")

        # ─── Try adding a panel ───
        print("13. Configuring a panel...")
        # The toggle buttons for cabinets should be visible
        time.sleep(1)
        screenshot(page, "panelen_ready")

        # Select a cabinet by clicking its toggle button
        spoelbak_btn = page.locator("button:has-text('Onderkast spoelbak')")
        if spoelbak_btn.count() > 0:
            spoelbak_btn.first.click()
            wait_for_blazor(page)
            screenshot(page, "paneel_selected_kast")

        # Fill panel name if there's a name input
        naam_input = page.locator("input[placeholder*='Midden'], input[placeholder*='paneel']")
        if naam_input.count() > 0:
            blazor_fill_loc(naam_input.first, "Deur spoelbak")

        # Add panel
        toev_btn = page.locator("button:has-text('Paneel toevoegen')")
        if toev_btn.count() > 0 and toev_btn.first.is_enabled():
            toev_btn.first.click()
            wait_for_blazor(page)
        screenshot(page, "paneel_added")

        # Try adding a multi-cabinet panel
        print("13b. Adding multi-cabinet panel...")
        spoelbak_btn = page.locator("button:has-text('Onderkast spoelbak')")
        vaatwasser_btn = page.locator("button:has-text('Onderkast vaatwasser')")
        if spoelbak_btn.count() > 0:
            spoelbak_btn.first.click()
            wait_for_blazor(page)
        if vaatwasser_btn.count() > 0:
            vaatwasser_btn.first.click()
            wait_for_blazor(page)
            screenshot(page, "multi_kast_selected")

        toev_btn = page.locator("button:has-text('Paneel toevoegen')")
        if toev_btn.count() > 0 and toev_btn.first.is_enabled():
            toev_btn.first.click()
            wait_for_blazor(page)
        screenshot(page, "multi_paneel_added")

        # ─── Navigate to Resultaat ───
        print("14. Navigating to Resultaat...")
        res_link = page.locator("a[href='/resultaat'], a:has-text('Resultaat')")
        if res_link.count() > 0:
            res_link.first.click()
            page.wait_for_url("**/resultaat")
            wait_for_blazor(page)
        screenshot(page, "resultaat")

        # ─── Final kasten view ───
        print("15. Final kasten check...")
        page.goto(f"{BASE_URL}/kasten")
        wait_for_blazor(page)
        screenshot(page, "kasten_final")

        print(f"\nDone! {step_counter} screenshots saved to {SCREENSHOT_DIR}")
        browser.close()


if __name__ == "__main__":
    main()
