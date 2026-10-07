"""Bake an original outline-triangle SDF fallback; no font downloads or Unity needed."""
from pathlib import Path
import math
import re

ROOT = Path(__file__).resolve().parents[1]
FONTS = ROOT / 'Assets/_Project/UI/Fonts'
GUID = 'f6e98c5fe7444fdfb5a7d17b92fb268b'
outer = ((16, 16), (56, 16), (36, 56))
inner = ((22, 20), (36, 48), (50, 20))

def inside(p, polygon):
    crosses = [(b[0]-a[0])*(p[1]-a[1])-(b[1]-a[1])*(p[0]-a[0])
               for a, b in zip(polygon, polygon[1:]+polygon[:1])]
    return all(c >= 0 for c in crosses) or all(c <= 0 for c in crosses)

def edge_distance(p, a, b):
    dx, dy = b[0]-a[0], b[1]-a[1]
    t = max(0, min(1, ((p[0]-a[0])*dx+(p[1]-a[1])*dy)/(dx*dx+dy*dy)))
    return math.hypot(p[0]-a[0]-t*dx, p[1]-a[1]-t*dy)

pixels = bytearray()
for y in range(128):
    for x in range(128):
        p = (x+.5, y+.5)
        distance = min(edge_distance(p, a, b) for poly in (outer, inner)
                       for a, b in zip(poly, poly[1:]+poly[:1]))
        signed = distance if inside(p, outer) and not inside(p, inner) else -distance
        pixels.append(round(max(0, min(1, .5+signed/16))*255))

text = (FONTS / 'InteractionPromptFont.asset').read_text(encoding='utf-8')
text = text.replace('Liberation Sans - Regular Atlas', 'CluePromptSymbols Atlas')
text = text.replace('Liberation Sans - Regular Material', 'CluePromptSymbols Material')
text = text.replace('m_Name: InteractionPromptFont', 'm_Name: CluePromptSymbols')
text = text.replace('m_FamilyName: Liberation Sans', 'm_FamilyName: Tikal Prompt Symbols')
for field in ('m_Width', 'm_Height', 'm_AtlasWidth', 'm_AtlasHeight'):
    text = re.sub(r'(?m)^  '+field+r': 1024$', '  '+field+': 128', text)
text = text.replace('m_CompleteImageSize: 1048576', 'm_CompleteImageSize: 16384')
text = text.replace('image data: 1048576', 'image data: 16384')
text = re.sub(r'(?m)^  _typelessdata: .*$', '  _typelessdata: '+pixels.hex(), text)
text = text.replace('_TextureHeight: 1024', '_TextureHeight: 128').replace('_TextureWidth: 1024', '_TextureWidth: 128')
text = re.sub(r'(?m)^  m_SourceFontFileGUID: .*$', '  m_SourceFontFileGUID: ', text)
glyph = '''  m_GlyphTable:
  - m_Index: 1
    m_Metrics:
      m_Width: 40
      m_Height: 40
      m_HorizontalBearingX: 0
      m_HorizontalBearingY: 40
      m_HorizontalAdvance: 42
    m_GlyphRect:
      m_X: 16
      m_Y: 16
      m_Width: 40
      m_Height: 40
    m_Scale: 1
    m_AtlasIndex: 0
    m_ClassDefinitionType: 0
  m_CharacterTable:
  - m_ElementType: 1
    m_Unicode: 9651
    m_GlyphIndex: 1
    m_Scale: 1
'''
text = re.sub(r'(?ms)^  m_GlyphTable:.*?(?=^  m_AtlasTextures:)', glyph, text)
text = re.sub(r'(?ms)^  m_UsedGlyphRects:.*?(?=^  m_FontFeatureTable:)',
              '  m_UsedGlyphRects: []\n  m_FreeGlyphRects: []\n', text)
text = re.sub(r'(?ms)^  m_FallbackFontAssetTable:.*?(?=^  m_FontWeightTable:)',
              '  m_FallbackFontAssetTable: []\n', text)
# The atlas is baked: never clear it at build time or add dynamic glyphs.
text = text.replace('m_IsMultiAtlasTexturesEnabled: 1', 'm_IsMultiAtlasTexturesEnabled: 0')
text = text.replace('m_ClearDynamicDataOnBuild: 1', 'm_ClearDynamicDataOnBuild: 0')
text = '\n'.join(line.rstrip() for line in text.splitlines()) + '\n'
(FONTS / 'CluePromptSymbols.asset').write_text(text, encoding='utf-8', newline='\n')
(FONTS / 'CluePromptSymbols.asset.meta').write_text(
    'fileFormatVersion: 2\nguid: '+GUID+'\nNativeFormatImporter:\n'
    '  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData:\n'
    '  assetBundleName:\n  assetBundleVariant:\n', encoding='utf-8', newline='\n')
print('Baked U+25B3: 128x128 Alpha8 SDF, static font fallback.')
