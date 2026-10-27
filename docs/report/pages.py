"""Helpers for build.js.

find <body.pdf> <items.json> <pages.json> : the page number of every heading, figure and table
join <front.pdf> <body.pdf> <pages.json> <out.pdf> : one PDF with bookmarks
"""
import json
import re
import sys
import unicodedata

from pypdf import PdfReader, PdfWriter


def squash(text):
    # pypdf reads some Thai marks back as \x00 or splits sara am, and lines break differently
    # from the HTML, so compare without spaces and without the Thai marks above and below a letter
    text = unicodedata.normalize("NFKC", text)
    return re.sub(r"[\s\x00\u0e31\u0e34-\u0e3a\u0e47-\u0e4e]+", "", text)


def find(body_pdf, items_json, pages_json):
    pages = [squash(p.extract_text() or "") for p in PdfReader(body_pdf).pages]
    items = json.load(open(items_json, encoding="utf-8"))
    at = 0
    for item in items:
        key = squash(item["title"])[:22]
        page = next((i for i in range(at, len(pages)) if key in pages[i]), None)
        if page is None:
            raise SystemExit("not found in the PDF : " + item["title"])
        item["page"] = page + 1
        at = page
    json.dump(items, open(pages_json, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print("pages found :", len(items), " body pages :", len(pages))


def join(front_pdf, body_pdf, pages_json, out_pdf):
    writer = PdfWriter()
    front = PdfReader(front_pdf)
    for p in front.pages:
        writer.add_page(p)
    shift = len(front.pages)
    for p in PdfReader(body_pdf).pages:
        writer.add_page(p)

    writer.add_outline_item("ปก", 0)
    writer.add_outline_item("คำนำ", 1)
    writer.add_outline_item("สารบัญ", 2)
    writer.add_outline_item("สารบัญรูปและตาราง", 3)
    chapter = None
    for item in json.load(open(pages_json, encoding="utf-8")):
        if item["kind"] == "ch":
            chapter = writer.add_outline_item(item["title"], shift + item["page"] - 1)
        elif item["kind"] == "sec":
            writer.add_outline_item(item["title"], shift + item["page"] - 1, parent=chapter)
    writer.add_metadata({"/Title": "A report of data structures used in game development 1", "/Author": "Alphodite"})
    writer.page_mode = "/UseOutlines"
    with open(out_pdf, "wb") as f:
        writer.write(f)
    print("pages :", len(writer.pages))


if __name__ == "__main__":
    if sys.argv[1] == "find":
        find(*sys.argv[2:5])
    else:
        join(*sys.argv[2:6])
