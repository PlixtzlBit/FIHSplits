using BepInEx;
using BepInEx.Unity.IL2CPP;
using UnityEngine;
using Il2CppInterop.Runtime.Injection;
using System;
using System.IO;
using System.Reflection;
using TMPro;
using EHS;
using EHS.Features.Multiplayer;
using System.Collections.Generic;

namespace com.plixtzlbit.fihsplits
{
    [BepInPlugin("com.plixtzlbit.fihsplits", "FIHSplits", "1.0.0")]
    public class FIHSplitsPlugin : BasePlugin
    {
        public override void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<FIHSplitsMain>();
            GameObject Obj = new GameObject("PBR-FIHSplits");
            UnityEngine.Object.DontDestroyOnLoad(Obj);
            Obj.AddComponent<FIHSplitsMain>();
        }
    }
    public class FIHSplitsMain : MonoBehaviour
    {
        public TextMeshProUGUI SplitInfo;
        Transform LocalPlayer;
        SyncedClock Clock;
        string PluginFolder;
        List<string> Names = new List<string>();
        Dictionary<string, double> BestTimes = new Dictionary<string, double>();
        Dictionary<string, double> SplitTimes = new Dictionary<string, double>();
        Dictionary<string, List<Vector3[]>> Boxes = new Dictionary<string, List<Vector3[]>>();
        HashSet<string> Triggered = new HashSet<string>();
        Vector3 TempPos1;
        Vector3 TempPos2;
        void Start()
        {
            PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string RulesetPath = Path.Combine(PluginFolder, "Ruleset.txt");
            string SplitsPath = Path.Combine(PluginFolder, "Splits.txt");
            if(!File.Exists(RulesetPath))
            {
				string T = @"Ms Elephants House
				-5.88,77.51,145.32|16.27,96.88,162.51
				Cardboard Fort
				-15.52,71.90,-56.26|22.97,85.82,-26.77
				First Playground
				9.17,97.01,-128.95|74.78,118.47,-83.73
				Pads
				-11.20,175.41,-116.76|6.42,200.86,-93.56
				Pyramid
				-6.46,216.74,-118.34|-1.77,229.84,-106.20
				Sanic
				-0.64,237.96,-120.96|22.66,246.76,-113.67
				Demo Cake
				-39.53,244.87,84.18|-30.57,250.85,90.66";
                File.WriteAllText(RulesetPath, T);
            }
            if(!File.Exists(SplitsPath))
            {
                string T = "";
                for(int i = 0; i < 7; i++)
                {
                    T += "Area " + (i + 1) + "\n-1\n";
                }
                File.WriteAllText(SplitsPath, T);
            }
        }
        void Update()
        {
            LocalPlayer = PlayerRef.LocalPlayer?.transform;
            Clock = UnityEngine.Object.FindObjectOfType<SyncedClock>();
            if(LocalPlayer != null)
            {
                if(Input.GetKeyDown(KeyCode.Comma))
                {
                    TempPos1 = LocalPlayer.position;
                }

                if(Input.GetKeyDown(KeyCode.Period))
                {
                    TempPos2 = LocalPlayer.position;
                }
                if(Input.GetKeyDown(KeyCode.Slash))
                {
                    Vector3 Min = new Vector3(
                        (float)Math.Round(Mathf.Min(TempPos1.x, TempPos2.x), 2),
                        (float)Math.Round(Mathf.Min(TempPos1.y, TempPos2.y), 2),
                        (float)Math.Round(Mathf.Min(TempPos1.z, TempPos2.z), 2)
                    );
                    Vector3 Max = new Vector3(
                        (float)Math.Round(Mathf.Max(TempPos1.x, TempPos2.x), 2),
                        (float)Math.Round(Mathf.Max(TempPos1.y, TempPos2.y), 2),
                        (float)Math.Round(Mathf.Max(TempPos1.z, TempPos2.z), 2)
                    );
                    File.WriteAllText(Path.Combine(PluginFolder, "Split Debug Trigger.txt"), $"{Min.x:F2},{Min.y:F2},{Min.z:F2}|{Max.x:F2},{Max.y:F2},{Max.z:F2}");
                }
            }
            if(SplitInfo == null)
            {
                GameUIManager Ui = UnityEngine.Object.FindObjectOfType<GameUIManager>();
                Canvas Canvas = Ui?.GetComponentInChildren<Canvas>();

                if(Canvas != null)
                {
                    GameObject Go = new GameObject("SplitInfo");
                    Go.transform.SetParent(Canvas.transform);
                    RectTransform Rect = Go.AddComponent<RectTransform>();
                    Rect.anchorMin = new Vector2(0, 1);
                    Rect.anchorMax = new Vector2(0, 1);
                    Rect.pivot = new Vector2(0, 1);
                    Rect.localPosition = new Vector3(-1010f, 239f, 0f);
                    Rect.sizeDelta = new Vector2(800f, 500f);
                    SplitInfo = Go.AddComponent<TextMeshProUGUI>();
                    SplitInfo.fontSize = 26;
                    SplitInfo.alignment = TextAlignmentOptions.TopLeft;
                    SplitInfo.enableWordWrapping = false;
                    Names.Clear();
                    BestTimes.Clear();
                    SplitTimes.Clear();
                    Boxes.Clear();
                    string[] Ruleset = File.ReadAllLines(Path.Combine(PluginFolder, "Ruleset.txt"));
                    string[] Splits = File.ReadAllLines(Path.Combine(PluginFolder, "Splits.txt"));
                    string Current = "";
                    for(int i = 0; i < Ruleset.Length; i++)
                    {
                        string Line = Ruleset[i].Trim();
                        if(string.IsNullOrEmpty(Line))
                        {
                            continue;
                        }
                        if(!Line.Contains("|"))
                        {
                            Current = Line;
                            if(!Names.Contains(Current))
                            {
                                Names.Add(Current);
                            }
                            if(!Boxes.ContainsKey(Current))
                            {
                                Boxes[Current] = new List<Vector3[]>();
                            }
                            continue;
                        }
                        var Parts = Line.Split('|');
                        var A = Parts[0].Split(',');
                        var B = Parts[1].Split(',');
                        float Ax = float.Parse(A[0]);
                        float Ay = float.Parse(A[1]);
                        float Az = float.Parse(A[2]);
                        float Bx = float.Parse(B[0]);
                        float By = float.Parse(B[1]);
                        float Bz = float.Parse(B[2]);
                        Vector3 Min = new Vector3(Mathf.Min(Ax, Bx), Mathf.Min(Ay, By), Mathf.Min(Az, Bz));
                        Vector3 Max = new Vector3(Mathf.Max(Ax, Bx), Mathf.Max(Ay, By), Mathf.Max(Az, Bz));
                        Boxes[Current].Add(new Vector3[] { Min, Max });
                    }
                    for(int i = 0; i + 1 < Splits.Length; i += 2)
                    {
                        string Name = Splits[i];
                        double Val = double.Parse(Splits[i + 1]);
                        BestTimes[Name] = Val;
                        SplitTimes[Name] = -1d;
                    }
                    foreach(var N in Names)
                    {
                        if(!BestTimes.ContainsKey(N))
                        {
                            BestTimes[N] = -1d;
                        }
                        if(!SplitTimes.ContainsKey(N))
                        {
                            SplitTimes[N] = -1d;
                        }
                    }
                }
                return;
            }
            if(LocalPlayer == null || Clock == null)
            {
                return;
            }
            Vector3 Pos = LocalPlayer.position;
            double Now = Clock.Time;
            foreach(var Pair in Boxes)
            {
                if(Triggered.Contains(Pair.Key))
                {
                    continue;
                }
                foreach(var Box in Pair.Value)
                {
                    Vector3 Min = Box[0];
                    Vector3 Max = Box[1];
                    if(Pos.x >= Min.x && Pos.x <= Max.x && Pos.y >= Min.y && Pos.y <= Max.y && Pos.z >= Min.z && Pos.z <= Max.z)
                    {
                        Triggered.Add(Pair.Key);
                        SplitTimes[Pair.Key] = Now;
                        double Best = BestTimes[Pair.Key];
                        if(Best < 0d || Now <= Best)
                        {
                            BestTimes[Pair.Key] = Now;
                            string T = "";
                            foreach(var N in Names)
                            {
                                T += N + "\n" + BestTimes[N] + "\n";
                            }
                            File.WriteAllText(Path.Combine(PluginFolder, "Splits.txt"), T);
                        }
                        break;
                    }
                }
            }
            string Text = "";
            foreach(var N in Names)
            {
                double Best = BestTimes[N];
                double Split = SplitTimes[N];
                Func<double, string> Format = (double Tm) =>
                {
                    if(Tm < 0d)
                    {
                        return "--:--.---";
                    }
                    int Total = (int)Tm;
                    int Min = Total / 60;
                    int Sec = Total % 60;
                    int Ms = (int)((Tm - Total) * 1000);
                    return Min.ToString("00") + ":" + Sec.ToString("00") + "." + Ms.ToString("000");
                };
                string Line = N + " | " + (Best < 0d ? "--:--.---" : Format(Best)) + " | ";
                if(Split < 0d)
                {
                    Line += "--:--.---";
                }
                else
                {
                    double Diff = Best < 0d ? 0d : Split - Best;
                    string Tf = Format(Split);
                    if(Best < 0d || Diff <= 0d)
                    {
                        Line += "<color=green>-" + Tf + "</color>";
                    }
                    else
                    {
                        Line += "<color=red>+" + Tf + "</color>";
                    }
                }
                Text += Line + "\n";
            }
            SplitInfo.text = Text;
        }
    }
}