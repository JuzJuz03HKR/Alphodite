// Render the loop diagrams to PNG with headless Chromium (Playwright).
// Run from the repo root :  NODE_PATH=$(npm root -g) node docs/diagrams/render.js
const { chromium } = require('playwright');
const path = require('path');
(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1600, height: 900 }, deviceScaleFactor: 2 });
  for (const name of ['core_game_loop', 'gameplay_loop']) {
    await page.goto('file://' + path.join(__dirname, name + '.html'));
    await page.waitForLoadState('networkidle');
    await page.evaluate(() => document.fonts.ready);
    await page.screenshot({ path: path.join(__dirname, name + '.png') });
    console.log('wrote', name + '.png');
  }
  await browser.close();
})();
