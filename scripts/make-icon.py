#!/usr/bin/env python3
"""生成 Thunderstore 要求的 256x256 icon.png（不依赖任何图形库）。"""
import pathlib
import struct
import zlib

W = H = 256
OUT = pathlib.Path(__file__).resolve().parent.parent / "Thunderstore" / "icon.png"

BG = (20, 26, 34)          # 深navy
PANEL = (32, 41, 54)
BAR = (120, 138, 160)
ACCENT = (232, 179, 74)    # RoR2 金币色
ITEM = [(238, 238, 238), (119, 255, 23), (231, 84, 58)]  # 白/绿/红 物品色


def rounded(x, y, rx, ry, rw, rh, r):
    """点在圆角矩形内？"""
    if not (rx <= x < rx + rw and ry <= y < ry + rh):
        return False
    cx = min(max(x, rx + r), rx + rw - 1 - r)
    cy = min(max(y, ry + r), ry + rh - 1 - r)
    return (x - cx) ** 2 + (y - cy) ** 2 <= r * r


def pixel(x, y):
    # 背板
    if not rounded(x, y, 16, 24, W - 32, H - 48, 24):
        return (0, 0, 0, 0)
    # 标题条
    if rounded(x, y, 32, 44, W - 64, 28, 12):
        return ACCENT + (255,)
    # 三行「玩家」
    for row in range(3):
        top = 96 + row * 42
        if rounded(x, y, 32, top, W - 64, 32, 10):
            # 行左侧的彩色方块 = 物品图标
            if rounded(x, y, 44, top + 8, 16, 16, 4):
                c = ITEM[row]
                return c + (255,)
            # 行右侧的两段条 = 名字/item 计数
            if rounded(x, y, 74, top + 10, 96 - row * 18, 12, 5):
                return BAR + (255,)
            if rounded(x, y, 190, top + 10, 34, 12, 5):
                return BAR + (255,)
            return PANEL + (255,)
    return BG + (255,)


raw = bytearray()
for y in range(H):
    raw.append(0)  # filter type 0
    for x in range(W):
        raw += bytes(pixel(x, y))


def chunk(tag, data):
    return (struct.pack(">I", len(data)) + tag + data
            + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))


png = (b"\x89PNG\r\n\x1a\n"
       + chunk(b"IHDR", struct.pack(">IIBBBBB", W, H, 8, 6, 0, 0, 0))
       + chunk(b"IDAT", zlib.compress(bytes(raw), 9))
       + chunk(b"IEND", b""))

OUT.parent.mkdir(parents=True, exist_ok=True)
OUT.write_bytes(png)
print(f"wrote {OUT} ({len(png)} bytes, {W}x{H})")
