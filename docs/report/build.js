// Build the data structures report into one PDF.
//   NODE_PATH=$(npm root -g) node docs/report/build.js
// Needs Playwright with Chromium, python3 with pypdf, and the Sarabun font installed.
// Steps : 1 print report.html (the chapters) with page numbers
//         2 find the page of every heading, figure and table (pages.py)
//         3 write front.html (cover, preface, contents) and print it
//         4 join the two PDFs and add bookmarks (pages.py join)
const { chromium } = require('playwright');
const { execFileSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const dir = __dirname;
const out = path.join(dir, 'build');
const finalPdf = path.join(dir, "TACET_DataStructures_Report1.pdf");
fs.mkdirSync(out, { recursive: true });

// Highlight : comments, strings and keywords in every <pre class="cs">, line by line
function highlight() {
  const words = ['public', 'private', 'static', 'class', 'struct', 'enum', 'new', 'void', 'int', 'float', 'bool',
    'string', 'return', 'if', 'else', 'for', 'while', 'continue', 'const', 'null', 'true', 'false', 'out'];
  const wordRe = new RegExp('\\b(' + words.join('|') + ')\\b', 'g');
  const esc = (t) => t.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  document.querySelectorAll('pre.cs').forEach((pre) => {
    const lines = pre.textContent.split('\n');
    pre.innerHTML = lines.map((line) => {
      let code = line, note = '';
      const cut = line.indexOf('//');
      if (cut >= 0) { code = line.slice(0, cut); note = line.slice(cut); }
      const parts = code.split(/("[^"]*"|'[^']*')/);
      const body = parts.map((p, i) => i % 2 ? '<span class="s">' + esc(p) + '</span>' : esc(p).replace(wordRe, '<span class="k">$1</span>')).join('');
      return body + (note ? '<span class="c">' + esc(note) + '</span>' : '');
    }).join('\n');
  });
}

const footer = '<div style="width:100%;text-align:center;font-family:Sarabun;font-size:10pt;color:#555;"><span class="pageNumber"></span></div>';

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();

  // 1 the chapters
  await page.goto('file://' + path.join(dir, 'report.html'));
  await page.evaluate(highlight);
  await page.evaluate(() => document.fonts.ready);
  const bodyPdf = path.join(out, 'body.pdf');
  await page.pdf({ path: bodyPdf, format: 'A4', preferCSSPageSize: true, displayHeaderFooter: true,
                   headerTemplate: '<div></div>', footerTemplate: footer, printBackground: true });

  // the headings, figure and table captions, in order, as the contents will list them
  const items = await page.evaluate(() => {
    const list = [];
    document.querySelectorAll('h1, h2, figcaption, table.tbl caption').forEach((el) => {
      let kind = el.tagName === 'H1' ? 'ch' : el.tagName === 'H2' ? 'sec' : el.tagName === 'FIGCAPTION' ? 'fig' : 'tab';
      let text = el.cloneNode(true);
      text.querySelectorAll('.tag').forEach((t) => t.remove());
      text.querySelectorAll('br').forEach((b) => b.replaceWith(' '));
      let title = text.textContent.replace(/\s+/g, ' ').trim();
      if (kind === 'ch') {
        const no = el.querySelector('.chno');
        title = no ? no.textContent.trim() + ' ' + title.replace(no.textContent.trim(), '').trim() : title;
      }
      list.push({ kind, title });
    });
    return list;
  });
  fs.writeFileSync(path.join(out, 'items.json'), JSON.stringify(items, null, 1));

  // 2 pages of each item
  execFileSync('python3', ['-I', path.join(dir, 'pages.py'), 'find', bodyPdf, path.join(out, 'items.json'), path.join(out, 'pages.json')], { stdio: 'inherit' });
  const found = JSON.parse(fs.readFileSync(path.join(out, 'pages.json'), 'utf8'));

  // 3 the front pages
  const row = (cls, title, no) => '<tr class="' + cls + '"><td class="title"><span class="lead">' + title + '</span></td><td class="num">' + no + '</td></tr>';
  let toc = '';
  found.filter((f) => f.kind === 'ch' || f.kind === 'sec').forEach((f) => { toc += row(f.kind, f.title, f.page); });
  let figs = '';
  found.filter((f) => f.kind === 'fig' || f.kind === 'tab').forEach((f) => {
    const short = f.title.length > 78 ? f.title.slice(0, 76).trim() + '...' : f.title;
    figs += row('sec', short, f.page);
  });
  const template = fs.readFileSync(path.join(dir, 'front.html'), 'utf8');
  const frontHtml = template.replace('<!--CONTENTS-->', toc).replace('<!--FIGURES-->', figs);
  const frontFile = path.join(out, 'front.filled.html');
  fs.writeFileSync(frontFile, frontHtml);
  fs.copyFileSync(path.join(dir, 'report.css'), path.join(out, 'report.css'));
  await page.goto('file://' + frontFile);
  await page.evaluate(() => document.fonts.ready);
  const frontPdf = path.join(out, 'front.pdf');
  await page.pdf({ path: frontPdf, width: '210mm', height: '297mm', margin: { top: 0, right: 0, bottom: 0, left: 0 }, printBackground: true });

  await browser.close();

  // 4 join, bookmarks
  execFileSync('python3', ['-I', path.join(dir, 'pages.py'), 'join', frontPdf, bodyPdf, path.join(out, 'pages.json'), finalPdf], { stdio: 'inherit' });
  console.log('done : ' + finalPdf);
})();
