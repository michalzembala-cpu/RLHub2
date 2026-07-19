using System;
using System.Globalization;
using System.Text;

namespace RLHub2.Models
{
    // One crosshair, as the set of cl_crosshair* cvars that define it.
    //
    // Commands() deliberately emits ONE line joined by semicolons. The CS2 console input is a
    // single-line field: pasting a multi-line block only ever runs the first line, which is why
    // the old multi-line copy appeared to do nothing.
    public class CrosshairDef
    {
        public string Name { get; set; } = "";
        public string Author { get; set; } = "";
        public int Style { get; set; } = 4;      // 4 = classic static (what almost every pro uses)
        public float Size { get; set; } = 2;
        public float Thickness { get; set; } = 1;
        public int Gap { get; set; } = -3;
        public bool Dot { get; set; }
        public bool T { get; set; }
        public int Outline { get; set; }          // 0 = off, else outline thickness
        public int R { get; set; }
        public int G { get; set; } = 255;
        public int B { get; set; }
        public int Alpha { get; set; } = 255;

        // Made in the app rather than shipped with it — only these can be edited or deleted.
        public bool Custom { get; set; }

        private static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        public string Commands()
        {
            var sb = new StringBuilder();
            void Add(string cmd) { if (sb.Length > 0) sb.Append("; "); sb.Append(cmd); }

            Add($"cl_crosshairstyle {Style}");
            Add($"cl_crosshairsize {F(Size)}");
            Add($"cl_crosshairthickness {F(Thickness)}");
            Add($"cl_crosshairgap {Gap}");
            Add($"cl_crosshairdot {(Dot ? 1 : 0)}");
            Add($"cl_crosshair_t {(T ? 1 : 0)}");
            Add($"cl_crosshair_drawoutline {(Outline > 0 ? 1 : 0)}");
            Add($"cl_crosshair_outlinethickness {Math.Max(0, Outline)}");
            Add("cl_crosshaircolor 5");
            Add($"cl_crosshaircolor_r {R}");
            Add($"cl_crosshaircolor_g {G}");
            Add($"cl_crosshaircolor_b {B}");
            Add($"cl_crosshairalpha {Alpha}");
            Add("cl_crosshairusealpha 1");
            return sb.ToString();
        }

        // Same cvars, one per line — for a .cfg file, where multi-line is the normal form.
        public string ConfigFile() => Commands().Replace("; ", Environment.NewLine) + Environment.NewLine;

        // The preview already shows dot / T / outline, so the line stays short and never clips.
        public string Summary() => $"size {Size:0.#}  •  gap {Gap}  •  thick {Thickness:0.#}";

        public CrosshairDef Clone() => (CrosshairDef)MemberwiseClone();

        // The classic cl_crosshaircolor palette, so a pasted config that uses a preset colour
        // instead of custom RGB still previews in the right colour.
        private static (int R, int G, int B)? Palette(int idx) => idx switch
        {
            0 => (255, 0, 0),
            1 => (0, 255, 0),
            2 => (255, 255, 0),
            3 => (0, 0, 255),
            4 => (0, 255, 255),
            _ => null,          // 5 = custom: leave the RGB alone
        };

        // Read a pasted blob of cl_crosshair* commands — one line, many lines, with or without
        // semicolons. Returns null if it contains nothing we recognise.
        public static CrosshairDef? Parse(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            var x = new CrosshairDef { Custom = true };
            bool any = false;

            // Held aside and applied at the end: the two outline cvars can arrive in either
            // order, and "drawoutline 0" must win over a leftover thickness whichever came first.
            bool? drawOutline = null;
            int outlineThickness = 0;

            var tokens = text.Split(new[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var raw in tokens)
            {
                var parts = raw.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;

                string cmd = parts[0].Trim('"').ToLowerInvariant();
                string val = parts[1].Trim('"');

                if (!float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                    continue;
                int i = (int)Math.Round(f);

                switch (cmd)
                {
                    case "cl_crosshairstyle": x.Style = i; any = true; break;
                    case "cl_crosshairsize": x.Size = f; any = true; break;
                    case "cl_crosshairthickness": x.Thickness = f; any = true; break;
                    case "cl_crosshairgap": x.Gap = i; any = true; break;
                    case "cl_crosshairdot": x.Dot = i != 0; any = true; break;
                    case "cl_crosshair_t": x.T = i != 0; any = true; break;
                    case "cl_crosshair_outlinethickness": outlineThickness = Math.Max(0, i); any = true; break;
                    case "cl_crosshairalpha": x.Alpha = Math.Clamp(i, 0, 255); any = true; break;
                    case "cl_crosshaircolor_r": x.R = Math.Clamp(i, 0, 255); any = true; break;
                    case "cl_crosshaircolor_g": x.G = Math.Clamp(i, 0, 255); any = true; break;
                    case "cl_crosshaircolor_b": x.B = Math.Clamp(i, 0, 255); any = true; break;

                    case "cl_crosshair_drawoutline": drawOutline = i != 0; any = true; break;

                    case "cl_crosshaircolor":
                        var pal = Palette(i);
                        if (pal != null) { x.R = pal.Value.R; x.G = pal.Value.G; x.B = pal.Value.B; }
                        any = true;
                        break;
                }
            }

            // Outline off => 0 regardless of thickness. Outline on but no thickness given => 1,
            // which is what CS2 defaults to.
            if (drawOutline == false) x.Outline = 0;
            else if (drawOutline == true) x.Outline = outlineThickness > 0 ? outlineThickness : 1;
            else x.Outline = outlineThickness;

            return any ? x : null;
        }
    }
}
