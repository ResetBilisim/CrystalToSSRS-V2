using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CrystalToSSRS.Licensing
{
    internal static class LicenseManager
    {
        private static HashSet<string> _validPlainKeys;
        private const string MasterKey = "CRSSRS-MASTER-UNLOCK"; // optional universal key

        private class State
        {
            public int Conversions { get; set; }
            public string LicenseKey { get; set; }
        }

        private static readonly string _stateDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CrystalToSSRS");
        private static readonly string _stateFile = Path.Combine(_stateDir, "license_state.json");
        private static State _state;
        public static bool HasValidLicense => _state != null && !string.IsNullOrEmpty(_state.LicenseKey) && IsKeyValid(_state.LicenseKey);

        public static void Init()
        {
            if (_state != null) return;
            LoadPlainKeys();
            LoadState();
        }

        private static void LoadState()
        {
            try
            {
                if (!Directory.Exists(_stateDir)) Directory.CreateDirectory(_stateDir);
                if (File.Exists(_stateFile))
                {
                    var json = File.ReadAllText(_stateFile);
                    _state = ParseState(json) ?? new State { Conversions = 0, LicenseKey = null };
                }
                else
                {
                    _state = new State { Conversions = 0, LicenseKey = null };
                    Save();
                }
            }
            catch
            {
                _state = new State { Conversions = 0, LicenseKey = null };
            }
        }

        private static State ParseState(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            // Extremely simple JSON parser (expects { "conversions": X, "licenseKey": "..." })
            int conversions = 0; string key = null;
            try
            {
                var lowered = json.ToLowerInvariant();
                int convIdx = lowered.IndexOf("conversions");
                if (convIdx >= 0)
                {
                    int colon = lowered.IndexOf(':', convIdx);
                    if (colon > 0)
                    {
                        var after = lowered.Substring(colon + 1);
                        var num = new string(after.TakeWhile(c => char.IsDigit(c)).ToArray());
                        int.TryParse(num, out conversions);
                    }
                }
                int keyIdx = lowered.IndexOf("licensekey");
                if (keyIdx >= 0)
                {
                    int colon = lowered.IndexOf(':', keyIdx);
                    if (colon > 0)
                    {
                        int quote1 = lowered.IndexOf('"', colon + 1);
                        if (quote1 >= 0)
                        {
                            int quote2 = lowered.IndexOf('"', quote1 + 1);
                            if (quote2 > quote1)
                            {
                                key = json.Substring(quote1 + 1, quote2 - quote1 - 1);
                            }
                        }
                    }
                }
                return new State { Conversions = conversions, LicenseKey = key };
            }
            catch { return null; }
        }

        private static string BuildStateJson(State s)
        {
            var sb = new StringBuilder();
            sb.Append("{\n  \"conversions\": ").Append(s.Conversions).Append(',').AppendLine();
            sb.Append("  \"licenseKey\": \"").Append(EscapeJson(s.LicenseKey ?? "")).Append("\"\n}");
            return sb.ToString();
        }

        private static string EscapeJson(string value)
        {
            if (value == null) return "";
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static void LoadPlainKeys()
        {
            if (_validPlainKeys != null) return;
            _validPlainKeys = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            try
            {
                var exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
                var jsonPath = Path.Combine(exeDir, "license_keys.json");
                if (File.Exists(jsonPath))
                {
                    var content = File.ReadAllText(jsonPath);
                    foreach (var k in ParseKeyList(content))
                        _validPlainKeys.Add(k);
                }
                else
                {
                    // Fallback: old text file format lic_keys.txt
                    var txtPath = Path.Combine(exeDir, "lic_keys.txt");
                    if (File.Exists(txtPath))
                    {
                        foreach (var line in File.ReadAllLines(txtPath))
                        {
                            var trimmed = line.Trim();
                            if (string.IsNullOrEmpty(trimmed)) continue;
                            var parts = trimmed.Split(',');
                            var key = parts[0].Trim();
                            if (key.StartsWith("CRSSRS-")) _validPlainKeys.Add(key);
                        }
                    }
                }
            }
            catch { }
            _validPlainKeys.Add(MasterKey); // Always include master key
        }

        private static IEnumerable<string> ParseKeyList(string json)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(json)) return list;
            try
            {
                // Accept either {"keys":["K1","K2"]} or simple ["K1","K2"]
                int arrayStart = json.IndexOf('[');
                int arrayEnd = json.IndexOf(']', arrayStart + 1);
                if (arrayStart >= 0 && arrayEnd > arrayStart)
                {
                    var arrayContent = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
                    var segments = arrayContent.Split(',');
                    foreach (var seg in segments)
                    {
                        var s = seg.Trim();
                        if (s.StartsWith("\"") && s.EndsWith("\""))
                        {
                            s = s.Substring(1, s.Length - 2);
                        }
                        if (s.StartsWith("CRSSRS-")) list.Add(s);
                    }
                }
            }
            catch { }
            return list;
        }

        private static bool IsKeyValid(string key)
        {
            LoadPlainKeys();
            return _validPlainKeys.Contains(key ?? "");
        }

        private static void Save()
        {
            try
            {
                if (!Directory.Exists(_stateDir)) Directory.CreateDirectory(_stateDir);
                File.WriteAllText(_stateFile, BuildStateJson(_state));
            }
            catch { }
        }

        public static bool AllowAnotherConversion()
        {
            Init();
            if (HasValidLicense) return true;
            return _state.Conversions < 1; // allow only 1 without license
        }

        public static void IncrementConversion()
        {
            Init();
            _state.Conversions++;
            Save();
        }

        public static bool TryActivate(string plainKey)
        {
            Init();
            if (string.IsNullOrWhiteSpace(plainKey)) return false;
            if (IsKeyValid(plainKey.Trim()))
            {
                _state.LicenseKey = plainKey.Trim();
                Save();
                return true;
            }
            return false;
        }

        public static void ShowActivationDialog(IWin32Window owner)
        {
            Init();
            using (var form = new Form())
            {
                form.Text = "Lisans";
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.Width = 380; form.Height = 170;
                form.MaximizeBox = false; form.MinimizeBox = false;

                var lbl = new Label { Left = 10, Top = 12, Width = 340, Text = HasValidLicense ? "Lisans etkin" : "Lisans anahtarını girin:" };
                var txt = new TextBox { Left = 10, Top = 35, Width = 340, Text = HasValidLicense ? _state.LicenseKey : "" };
                var btnOk = new Button { Text = HasValidLicense ? "Güncelle" : "Aktive Et", Left = 185, Top = 70, Width = 80, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Kapat", Left = 270, Top = 70, Width = 80, DialogResult = DialogResult.Cancel };
                var info = new Label { Left = 10, Top = 105, Width = 340, ForeColor = System.Drawing.Color.DimGray, Text = "Deneme: 1 ücretsiz dönüşüm. JSON: license_keys.json" };

                btnOk.Click += (s, e) =>
                {
                    if (TryActivate(txt.Text.Trim()))
                    {
                        MessageBox.Show(form, "Lisans aktive edildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        form.DialogResult = DialogResult.OK;
                        form.Close();
                    }
                    else
                    {
                        MessageBox.Show(form, "Anahtar geçersiz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                form.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel, info });
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;
                form.ShowDialog(owner);
            }
        }
    }
}
