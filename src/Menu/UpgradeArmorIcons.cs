using System;
using SFML.Graphics;

namespace MMXOnline;

public partial class UpgradeArmorMenu {
	static Texture? purchasedArmorIcons;
	static AnimData? purchasedArmorIconSource;
	const int armorIconCellWidth = 32;

	static void preparePurchasedArmorIcons() {
		if (!Global.sprites.TryGetValue("newmenu_xarmors", out var source) ||
			ReferenceEquals(source, purchasedArmorIconSource)) return;
		using var original = source.bitmap.CopyToImage();
		using var atlas = new Image(((uint)(source.frames.Length * armorIconCellWidth), 30), Color.Transparent);
		for (int frame = 0; frame < source.frames.Length; frame++) {
			var rect = source.frames[frame].rect;
			int width = (int)rect.w();
			int height = (int)rect.h();
			if (width > armorIconCellWidth || height != 30) return;
			atlas.Copy(original, ((uint)(frame * armorIconCellWidth), 0),
				new IntRect(((int)rect.x1, (int)rect.y1), (width, height)));
			// This sheet puts the frame in its top/bottom six rows and side studs.
			for (int y = 6; y < 24; y++) {
				for (int x = 0; x < width; x++) {
					bool sideStud = (x < 3 || x >= width - 3) &&
						(y is >= 7 and <= 9 || y is >= 20 and <= 22);
					if (sideStud) continue;
					var point = new SFML.System.Vector2u((uint)(frame * armorIconCellWidth + x), (uint)y);
					Color color = atlas.GetPixel(point);
					color.A = (byte)(color.A * 0.35f);
					atlas.SetPixel(point, color);
				}
			}
		}
		purchasedArmorIcons?.Dispose();
		purchasedArmorIcons = new Texture(atlas);
		purchasedArmorIconSource = source;
	}

	static void drawArmorIcon(AnimData source, int frame, Point pos, bool purchased, bool equipped) {
		if (!purchased || equipped || purchasedArmorIcons == null) {
			source.drawToHUD(frame, pos.x, pos.y, equipped ? 1 : 0.35f);
			return;
		}
		var data = source.frames[frame];
		float originX = MathF.Floor(source.baseAlignmentX * data.rect.w() + source.alignOffX - data.offset.x);
		float originY = MathF.Floor(source.baseAlignmentY * data.rect.h() + source.alignOffY - data.offset.y);
		DrawWrappers.DrawTextureHUD(purchasedArmorIcons, frame * armorIconCellWidth, 0,
			data.rect.w(), data.rect.h(), pos.x - originX, pos.y - originY);
	}
}
