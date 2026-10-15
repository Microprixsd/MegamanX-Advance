using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.System;

namespace MMXOnline;

// Skill purchases share the player's existing tank inventory.
public class XSkillTreeMenu : IMainMenu {
	readonly IMainMenu prevMenu;
	readonly bool planning;
	readonly List<string> route = new();
	readonly string[] orderLabels = new string[nodes.Length];
	XSkillProgress preview = new();
	XSkillProgress progress => planning ? preview : mainPlayer.xSkills;
	Player? purchasePlayer => planning ? null : mainPlayer;
	readonly ConnectionMesh connections;
	readonly Point[] nodePositions = new Point[nodes.Length];
	int selectedNode;
	int pendingPurchase = -1;
	string[] descriptionLines = Array.Empty<string>();
	string selectedName = "";
	static readonly string[] activityLabels = { "ATK", "DEF", "SP", "MAP" };
	// Final HUD rows preserve the tuned pixel spacing; X uses a shared column grid.
	readonly record struct SkillNode(string id, string label, float x, float y, int palette, int frame);

	static (string name, string description) getSkillDetails(SkillNode node) => node.id switch {
		"bp1" => ("Buster Plus (1)", "After firing 10 lemons, the next one will have its damage doubled."),
		"bp2" => ("Buster Plus (2)", "After firing 8 lemons, the next one will have its damage doubled."),
		"bp3" => ("Buster Plus (3)", "After firing 6 lemons, the next one will have its damage doubled."),
		"sb1" => ("Shock Buffer (1)", "Reduces damage by 12.5% without armor."),
		"sb2" => ("Shock Buffer (2)", "Reduces flinch time by 12.5%."),
		"s1" => ("Speedster (1)", "Increases grounded walk/run speed by 10%. Excludes dashes."),
		"s2" => ("Speedster (2)", "Increases normal air movement speed by 10%. Excludes dashes."),
		"hd1" => ("Hyper Dash (1)", "Increases ground dash speed by 7%."),
		"hd2" => ("Hyper Dash (2)", "Increases air dash speed by 7%."),
		"sp1" => ("Saber Plus (1)", "Adds 1 damage to X's ground and air Z-Saber."),
		"sp2" => ("Saber Plus (2)", "X's Z-Saber ignores defense reductions."),
		"sp3" => ("Saber Plus (3)", "Adds mini flinch to X's Z-Saber and 0.5s to its cooldown."),
		_ => node.label switch {
			"X" => ("X", "You can earn up to 999 Skill Points, but only 90 can be spent on upgrades. Choose your build wisely!"),
			"H" => ($"Heart Tank ({node.id[^1]})", "Permanently increases X's maximum HP by 5."),
			"S" when node.palette == 5 => ($"Sub Tank ({node.id[^1]})", "Stores health energy that can be used later to recover HP."),
			"L.U" => ("Life Up", "Increases X's maximum HP by 7."),
			"E.U" => ("Energy Up", "Increases the maximum energy capacity of X's weapons by 4."),
			"D.B" => ("Double Barrier", "Grants 0.6s of invulnerability after taking damage from pits or spikes."),
			"D.C" => ("D-Converter", "Every 8 HP lost restores 1 ammo to all special weapons. No giga attacks."),
			"J" => ($"Jumper ({node.id[^1]})", $"Increases original jump height by {(node.id[^1] - '0') * 5}% total."),
			"E.S" => ($"Energy Saver ({node.id[^1]})", $"Reduces base ammo regen time by {(node.id[^1] - '0') * 10}%. No giga attacks."),
			"S.R" => ("Super Recover", "Recovery items restore 25% more health or weapon energy."),
			"U.B" => ("Ultimate Buster", "Automatically charges with any weapon equipped. Press fire to shoot."),
			"Q.C" => ("Quick Charge", "Increases X-Buster charge speed by 12.5%."),
			"S.S" => ("Speed Shot", "Lemons and charged X-Buster shots travel 20% faster."),
			"S.E" => ("Shot Eraser", "X's Z-Saber deflects projectiles. Adds 0.5s to its cooldown."),
			_ => (node.label, "")
		}
	};

	static readonly Color[] colors = {
		new(196, 207, 218), new(255, 65, 82), new(255, 218, 76),
		new(79, 212, 224), new(168, 255, 91), new(255, 139, 66), new(174, 89, 237)
	};
	static readonly SkillNode[] nodes = {
		new("root", "X", 420, 183, 0, 0),
		new("bp1", "B.P", 220, 162, 1, 15),
		new("sp1", "S.P", 100, 162, 1, 16),
		new("sp2", "S.P", 40, 140, 1, 16),
		new("sp3", "S.P", 40, 118, 1, 16),
		new("se", "S.E", 100, 140, 1, 18),
		new("bp2", "B.P", 160, 140, 1, 15),
		new("ss", "S.S", 220, 140, 1, 17),
		new("qc", "Q.C", 280, 140, 1, 14),
		new("ub", "U.B", 220, 118, 1, 13),
		new("bp3", "B.P", 100, 118, 1, 15),
		new("s1", "S", 420, 159, 2, 7),
		new("s2", "S", 360, 159, 2, 7),
		new("j1", "J", 360, 140, 2, 9),
		new("j2", "J", 420, 140, 2, 9),
		new("hd1", "H.D", 360, 121, 2, 8),
		new("hd2", "H.D", 360, 102, 2, 8),
		new("db", "D.B", 360, 83, 6, 5),
		new("es1", "E.S", 500, 159, 3, 10),
		new("es2", "E.S", 500, 140, 3, 10),
		new("es3", "E.S", 500, 121, 3, 10),
		new("sr", "S.R", 440, 102, 3, 12),
		new("dc", "D.C", 560, 102, 3, 6),
		new("eu", "E.U", 560, 63, 3, 4),
		new("lu", "L.U", 620, 63, 4, 3),
		new("sb1", "S.B", 590, 43, 6, 11),
		new("sb2", "S.B", 650, 43, 6, 11),
		new("h1", "H", 640, 159, 4, 1),
		new("h2", "H", 640, 141, 4, 1),
		new("h3", "H", 640, 123, 4, 1),
		new("h4", "H", 640, 105, 4, 1),
		new("h5", "H", 640, 87, 4, 1),
		new("shield1", "S", 760, 159, 5, 2),
		new("shield2", "S", 760, 123, 5, 2),
		new("shield3", "S", 760, 105, 5, 2),
		new("shield4", "S", 760, 87, 5, 2),
	};
	// Orthogonal forks use matching left/right arms and centered node anchors.
	static readonly float[][] paths = {
		new float[] {220, 162, 220, 172, 640, 172, 640, 141},
		new float[] {420, 183, 420, 159, 360, 159, 360, 83},
		new float[] {500, 172, 500, 110},
		new float[] {220, 162, 100, 162, 100, 151, 40, 151, 40, 140},
		new float[] {100, 151, 100, 140},
		new float[] {40, 140, 40, 118},
		new float[] {220, 162, 220, 118},
		new float[] {220, 151, 160, 151, 160, 129, 280, 129, 280, 151, 220, 151},
		new float[] {100, 118, 100, 129, 160, 129},
		new float[] {360, 140, 420, 140},
		new float[] {500, 110, 470, 110, 470, 102, 440, 102, 470, 102, 470, 94, 530, 94, 530, 102, 560, 102, 530, 102, 530, 110, 500, 110},
		new float[] {500, 94, 500, 76, 560, 76, 560, 63, 620, 63},
		new float[] {590, 63, 590, 43, 650, 43},
		new float[] {620, 63, 620, 75, 700, 75, 700, 80},
		new float[] {700, 80, 670, 80, 670, 87, 640, 87, 670, 87, 670, 94, 730, 94, 730, 87, 760, 87, 730, 87, 730, 80, 700, 80},
		new float[] {700, 94, 700, 98, 670, 98, 670, 105, 640, 105, 670, 105, 670, 112, 730, 112, 730, 105, 760, 105, 730, 105, 730, 98, 700, 98},
		new float[] {700, 112, 700, 116, 670, 116, 670, 123, 640, 123, 670, 123, 670, 130, 730, 130, 730, 123, 760, 123, 730, 123, 730, 116, 700, 116},
		new float[] {700, 130, 700, 141, 640, 141},
		new float[] {640, 159, 760, 159},
	};
	Player mainPlayer => Global.level.mainPlayer;

	public XSkillTreeMenu(IMainMenu prevMenu, bool planning = false) {
		this.planning = planning;
		if (planning) {
			if (Options.main.xSkillTreeRoute != null) route.AddRange(Options.main.xSkillTreeRoute);
			rebuildPreview();
		}
		// Keep Back pointed at the pause menu when entered through an armor page.
		this.prevMenu = prevMenu is UpgradeMenu items ? items.prevMenu : prevMenu;
		for (int i = 0; i < nodes.Length; i++) {
			nodePositions[i] = screenPoint(nodes[i].x, nodes[i].y);
		}
		connections = buildConnections();
		updateDescription();
	}

	void rebuildPreview() {
		preview = new XSkillProgress { points = XSkillProgress.maxPoints };
		Array.Clear(orderLabels, 0, orderLabels.Length);
		for (int i = 0; i < route.Count; i++) {
			if (!preview.planPurchase(route[i])) {
				route.RemoveRange(i, route.Count - i);
				break;
			}
			int node = Array.FindIndex(nodes, n => n.id == route[i]);
			if (node >= 0) orderLabels[node] = (i + 1).ToString();
		}
	}

	void saveRoute() {
		if (!planning) return;
		Options.main.xSkillTreeRoute = new List<string>(route);
		Options.main.saveToFile();
	}

	public void update() {
		if (Global.input.isPressedMenu(Control.MenuBack)) {
			saveRoute();
			Menu.change(prevMenu);
			return;
		}
		if (Global.input.isPressedMenu(Control.WeaponLeft) || Global.input.isPressedMenu(Control.WeaponRight)) {
			saveRoute();
			Menu.change(planning ? (Global.input.isPressedMenu(Control.WeaponRight) ?
				new QuickArmorSwitchMenu(prevMenu) : prevMenu) :
				(Global.input.isPressedMenu(Control.WeaponRight)
					? new ConsumableShopMenu(prevMenu) : new UpgradeArmorMenu(prevMenu)));
			return;
		}
		if (Global.input.isPressedMenu(Control.MenuLeft)) moveSelection(-1, 0);
		else if (Global.input.isPressedMenu(Control.MenuRight)) moveSelection(1, 0);
		else if (Global.input.isPressedMenu(Control.MenuUp)) moveSelection(0, -1);
		else if (Global.input.isPressedMenu(Control.MenuDown)) moveSelection(0, 1);
		if (planning) {
			string id = nodes[selectedNode].id;
			if (Global.input.isPressedMenu(Control.MenuConfirm) && preview.planPurchase(id)) {
				route.Add(id);
				rebuildPreview();
			} else if (Global.input.isPressedMenu(Control.MenuAlt)) {
				int index = id == "root" ? 0 : route.IndexOf(id);
				if (index >= 0 && index < route.Count) {
					route.RemoveRange(index, route.Count - index);
					rebuildPreview();
				}
			}
			return;
		}
		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			string id = nodes[selectedNode].id;
			if (mainPlayer.xSkills.purchaseStatus(mainPlayer, id) != "Buy") {
				pendingPurchase = -1;
			} else if (pendingPurchase == selectedNode) {
				mainPlayer.xSkills.purchase(mainPlayer, id);
				pendingPurchase = -1;
			} else {
				pendingPurchase = selectedNode;
			}
		}
	}

	// Choose the closest node in the pressed direction, favoring aligned nodes.
	void moveSelection(int dx, int dy) {
		Point origin = nodePositions[selectedNode];
		float bestScore = float.MaxValue;
		int next = selectedNode;
		for (int i = 0; i < nodes.Length; i++) {
			Point target = nodePositions[i];
			float x = target.x - origin.x;
			float y = target.y - origin.y;
			float forward = x * dx + y * dy;
			float sideways = MathF.Abs(x * dy - y * dx);
			if (forward <= 1 || sideways > forward * 2) continue;
			float score = forward + sideways * 3;
			if (score < bestScore) {
				bestScore = score;
				next = i;
			}
		}
		if (selectedNode != next) {
			selectedNode = next;
			pendingPurchase = -1;
			updateDescription();
		}
	}

	void updateDescription() {
		// Wrap once per selection, using actual font widths rather than character counts.
		var details = getSkillDetails(nodes[selectedNode]);
		selectedName = details.name;
		if (nodes[selectedNode].id == "root") {
			if (planning) {
				descriptionLines = new[] { "Plan up to 90 S.P.", "Numbers show order.", "Enable automatic", "skills in X settings.", "ALT on X clears", "the whole route." };
				return;
			}
			descriptionLines = new[] { "You can earn up to", "999 Skill Points,", "but only 90 can be",
				"spent on upgrades.", "Choose your build", "wisely!" };
			return;
		}
		List<string> lines = new();
		string line = "";
		foreach (string word in details.description.Split(' ', StringSplitOptions.RemoveEmptyEntries)) {
			string candidate = line.Length == 0 ? word : line + " " + word;
			if (line.Length > 0 && Fonts.measureText(FontType.Grey, candidate) > 128) {
				lines.Add(line);
				line = word;
			} else {
				line = candidate;
			}
		}
		if (line.Length > 0) lines.Add(line);
		descriptionLines = lines.ToArray();
	}

	static Point screenPoint(float x, float y) {
		float columnSpacing = MathF.Floor((Global.screenW - 72) / 12f);
		return new Point(MathF.Round(34 + (x - 40) / 60 * columnSpacing), y);
	}

	public void render() {
		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		Fonts.drawText(FontType.Yellow, planning ? "X Skill Route" : "X Skill Tree", Global.halfScreenW, 12, Alignment.Center);
		if (!planning) Fonts.drawText(FontType.Grey, $"Spent: {progress.spentPoints}/90", Global.screenW - 24, 12, Alignment.Right);
		DrawWrappers.DrawRect(20, 23, Global.screenW - 20, 194, true,
			new Color(13, 23, 39, 230), 1, ZIndex.HUD, false, outlineColor: new Color(62, 89, 116));
		DrawWrappers.drawToHUD(connections);
		Global.sprites.TryGetValue("X_Skilltree", out var chips);
		for (int i = 0; i < nodes.Length; i++) drawNode(i, chips);
		if (planning) {
			for (int i = 0; i < nodes.Length; i++) {
				if (orderLabels[i] != null) Fonts.drawText(FontType.Yellow, orderLabels[i],
					nodePositions[i].x + 8, nodePositions[i].y - 8);
			}
		}
		DrawWrappers.DrawRect(25, 28, 161, 42, true,
			new Color(8, 16, 29, 240), 1, ZIndex.HUD, false, outlineColor: new Color(62, 89, 116));
		Fonts.drawText(FontType.Yellow, planning ? "Points used:" : "Skill Points:", 29, 32);
		Fonts.drawText(FontType.Blue, planning ? $"{preview.spentPoints}/90" : progress.points.ToString(), 157, 32, Alignment.Right);
		if (!planning) {
			DrawWrappers.DrawRect(166, 28, 244, 73, true,
				new Color(8, 16, 29, 240), 1, ZIndex.HUD, false, outlineColor: new Color(62, 89, 116));
			for (int i = 0; i < activityLabels.Length; i++) {
				var activity = (XSkillActivity)i;
				float y = 32 + i * 10;
				Fonts.drawText(FontType.Yellow, $"{activityLabels[i]} {progress.getLevel(activity)}", 170, y);
				DrawWrappers.DrawRect(220, y + 2, 240, y + 5, true, new Color(35, 52, 73), 1, ZIndex.HUD, false);
				float fill = (float)progress.getExperiencePercent(activity) / 100;
				if (fill > 0) DrawWrappers.DrawRect(220, y + 2, 220 + 20 * fill, y + 5, true,
					new Color(79, 212, 224), 1, ZIndex.HUD, false);
			}
		}
		DrawWrappers.DrawRect(25, 46, 161, 108, true,
			new Color(8, 16, 29, 240), 1, ZIndex.HUD, false, outlineColor: new Color(62, 89, 116));
		Fonts.drawText(FontType.Yellow, selectedName, 29, 51);
		for (int i = 0; i < descriptionLines.Length; i++) {
			bool root = nodes[selectedNode].id == "root";
			Fonts.drawText(root && (i == 1 || i == 2 || i >= 4) ? FontType.Yellow : FontType.Grey,
				descriptionLines[i], 29, root ? 59 + i * 8 : 65 + i * 10);
		}
		string nodeId = nodes[selectedNode].id;
		int cost = XSkillProgress.getCost(nodeId);
		string status = progress.purchaseStatus(purchasePlayer, nodeId);
		if (cost > 0) {
			Fonts.drawText(FontType.Grey, $"Costs {cost} S.P", 25, 186);
			string hint = pendingPurchase == selectedNode && status == "Buy" ? "[OK]: Confirm" :
				status == "Buy" ? "[OK]" : status == "Not enough SP" || status == "Locked" ? "" : status;
			if (planning) hint = status == "Buy" ? "[OK]: Add" : status == "Purchased" ? "[ALT]: Remove this + next" : status;
			Fonts.drawTextEX(FontType.Grey, hint, Global.screenW - 25, 186, Alignment.Right);
		}
		Fonts.drawTextEX(FontType.Grey, "[MLEFT]/[MRIGHT]/[MUP]/[MDOWN]: Select", Global.halfScreenW, 196, Alignment.Center);
		Fonts.drawTextEX(FontType.Grey, "[WeaponL]/[WeaponR]: Page  [BACK]: Back", Global.halfScreenW, 206, Alignment.Center);
	}

	static ConnectionMesh buildConnections() {
		// Rasterize once onto the HUD pixel grid. Shared pixels merge at corners and
		// intersections, so separate diagonal polygons cannot leave seams behind.
		HashSet<(int x, int y)> light = new();
		HashSet<(int x, int y)> shadow = new();
		foreach (float[] path in paths) {
			for (int i = 2; i < path.Length; i += 2) {
				Point from = screenPoint(path[i - 2], path[i - 1]);
				Point to = screenPoint(path[i], path[i + 1]);
				int x = (int)from.x, y = (int)from.y;
				int endX = (int)to.x, endY = (int)to.y;
				if (x != endX && y != endY) {
					throw new InvalidOperationException("Skill tree connections must be horizontal or vertical.");
				}
				int sx = Math.Sign(endX - x), sy = Math.Sign(endY - y);
				while (true) {
					// Center the two-pixel stroke on the same anchor as the 16x16 icon.
					light.Add((x - 1, y - 1));
					shadow.Add((x - 1, y - 1));
					shadow.Add((x, y - 1));
					shadow.Add((x - 1, y));
					shadow.Add((x, y));
					if (x == endX && y == endY) break;
					x += sx;
					y += sy;
				}
			}
		}
		List<Vertex> vertices = new();
		// Remove strokes beneath the full icon bounds, including transparent pixels.
		// This keeps dimmed chips from showing the connections through their artwork.
		Global.sprites.TryGetValue("X_Skilltree", out var chips);
		foreach (SkillNode node in nodes) {
			Point center = screenPoint(node.x, node.y);
			int width = 16, height = 16;
			if (chips != null && node.frame < chips.frames.Length) {
				width = (int)chips.frames[node.frame].rect.w();
				height = (int)chips.frames[node.frame].rect.h();
			}
			int left = (int)center.x - width / 2, top = (int)center.y - height / 2;
			for (int y = top; y < top + height; y++) {
				for (int x = left; x < left + width; x++) {
					shadow.Remove((x, y));
					light.Remove((x, y));
				}
			}
		}
		appendConnectionVertices(shadow, new Color(35, 104, 216), vertices);
		appendConnectionVertices(light, new Color(100, 215, 255), vertices);
		return new ConnectionMesh(vertices.ToArray());
	}

	static void appendConnectionVertices(HashSet<(int x, int y)> pixels, Color color, List<Vertex> vertices) {
		List<(int x, int y, int width)> spans = new();
		var sorted = new List<(int x, int y)>(pixels);
		sorted.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
		foreach (var pixel in sorted) {
			if (spans.Count > 0) {
				var last = spans[spans.Count - 1];
				if (last.y == pixel.y && last.x + last.width == pixel.x) {
					spans[spans.Count - 1] = (last.x, last.y, last.width + 1);
					continue;
				}
			}
			spans.Add((pixel.x, pixel.y, 1));
		}
		foreach (var span in spans) {
			Vertex topLeft = new(new Vector2f(span.x, span.y), color);
			Vertex topRight = new(new Vector2f(span.x + span.width, span.y), color);
			Vertex bottomLeft = new(new Vector2f(span.x, span.y + 1), color);
			Vertex bottomRight = new(new Vector2f(span.x + span.width, span.y + 1), color);
			vertices.Add(topLeft); vertices.Add(topRight); vertices.Add(bottomLeft);
			vertices.Add(topRight); vertices.Add(bottomRight); vertices.Add(bottomLeft);
		}
	}

	// Managed vertex data needs no native resource disposal. Draw all connections
	// in one call, with the blue layer before the cyan layer as in the original.
	sealed class ConnectionMesh(Vertex[] vertices) : IDrawable {
		public void Draw(IRenderTarget target, RenderStates states) {
			target.Draw(vertices, PrimitiveType.Triangles, states);
		}
	}

	void drawNode(int index, AnimData? chips) {
		SkillNode node = nodes[index];
		Point p = nodePositions[index];
		bool selected = index == selectedNode;
		int frame = node.frame;
		bool purchased = progress.isPurchased(purchasePlayer, node.id);
		float alpha = purchased || node.id == "root" ? 1 : 0.35f;
		if (chips != null &&
			frame >= 0 && frame < chips.frames.Length) {
			chips.drawToHUD(frame, p.x, p.y, alpha);
			if (selected) {
				float halfWidth = MathF.Ceiling(chips.frames[frame].rect.w() / 2f) + 1;
				float halfHeight = MathF.Ceiling(chips.frames[frame].rect.h() / 2f) + 1;
				DrawWrappers.DrawRect(p.x - halfWidth, p.y - halfHeight, p.x + halfWidth, p.y + halfHeight,
					false, Color.White, 1, ZIndex.HUD, false);
			}
			return;
		}
		// Keep the menu usable if an older asset pack is loaded.
		float radius = 8;
		float width = Math.Max(8, Fonts.measureText(FontType.Blue, node.label) / 2f + 2);
		List<Point> points = new() {
			new(p.x, p.y - radius), new(p.x + width, p.y - radius / 2),
			new(p.x + width, p.y + radius / 2), new(p.x, p.y + radius),
			new(p.x - width, p.y + radius / 2), new(p.x - width, p.y - radius / 2)
		};
		Color nodeColor = colors[node.palette];
		nodeColor.A = (byte)(255 * alpha);
		DrawWrappers.DrawPolygon(points, nodeColor, true, ZIndex.HUD, false,
			outlineColor: selected ? Color.White : null, thickness: 1);
		Fonts.drawText(FontType.Blue, node.label, p.x, p.y - 4, Alignment.Center, alpha: (byte)(255 * alpha));
	}
}
