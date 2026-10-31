"""The approved store copy in Store/listing-en-US.md (D-030), for one platform.

Sentences and lines marked (iOS) belong to the App Store only and those marked (Android) to Google Play
only, so neither store's listing names the other platform. Both store scripts read their copy here.
"""
import re
from pathlib import Path

LISTING = Path(__file__).resolve().parent.parent / "Store/listing-en-US.md"
MARKERS = {"ios": "(iOS)", "android": "(Android)"}


def copy_for(platform):
    """The listing fields and description for "ios" or "android"."""
    keep, drop = MARKERS[platform], MARKERS["android" if platform == "ios" else "ios"]
    text = LISTING.read_text()
    field = lambda label: text.split(f"**{label}:** ", 1)[1].split("\n", 1)[0].strip()
    kept = []
    for paragraph in text.split("## Description\n", 1)[1].strip().split("\n\n"):
        lines = paragraph.split("\n")
        # A marker follows its sentence's full stop, so a sentence ends at a full stop not followed by a marker,
            # or after a marker; each marker stays with the sentence it marks.
        body = []
        for line in lines[1:] if len(lines) > 1 else []:
            sentences = re.split(r"(?<=\.) (?!\((?:iOS|Android)\))|(?<=\(iOS\)) |(?<=\(Android\)) ", line)
            line = " ".join(s for s in sentences if drop not in s).replace(" " + keep, "").replace(keep, "").rstrip()
            if line.strip():
                body.append(line)
        if body or len(lines) == 1:
            kept.append("\n".join([lines[0]] + body))
    return {
        "name": field("Name / title"),
        "subtitle": field("App Store subtitle (30)"),
        "shortDescription": field("Google Play short description (80)"),
        "promotionalText": field("App Store promotional text (170)"),
        "keywords": field("App Store keywords (100)"),
        "description": "\n\n".join(kept),
        "whatsNew": field("App Store What's New"),
        "releaseNotes": field("Google Play release notes"),
    }
