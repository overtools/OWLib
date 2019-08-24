using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DataTool.DataModels;
using DataTool.ToolLogic.Extract;
using DataTool.WPF.IO;
using DirectXTexNet;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;
using static DataTool.Program;

namespace DataTool.WPF.Tool.Export {
    public static class HeroUnlocksView {
        public static Task<Control> Get(ProgressWorker a1, SynchronizationContext context, Window window, bool npc) {
            var source = new TaskCompletionSource<Control>();

            context.Send(obj => {
                var control = new ImageGridView();
                var t = new Thread(() => {
                    if (!(obj is Tuple<ProgressWorker, TaskCompletionSource<Control>> tuple)) return;
                    var worker = tuple.Item1;
                    var tcs = tuple.Item2;
                    try {
                        var i = 0;
                        worker.ReportProgress(0, "Loading heroes...");
                        if (TrackedFiles == null || !TrackedFiles.ContainsKey(0x75)) {
                            throw new DataToolWpfException("Open storage first");
                        }
                        
                        var max = TrackedFiles[0x75].Count;

                        foreach (var key in TrackedFiles[0x75]) {
                            try {
                                var hero = GetInstance<STUHero>(key);
                                if (hero == null) continue;
                                string heroNameActual = GetString(hero.m_0EDCE350) ?? teResourceGUID.Index(key).ToString("X");

                                heroNameActual = heroNameActual.TrimEnd(' ');

                                ProgressionUnlocks progressionUnlocks = new ProgressionUnlocks(hero);
                                if (progressionUnlocks.LevelUnlocks == null && !npc) {
                                    continue;
                                }
                                if (progressionUnlocks.LootBoxesUnlocks != null && npc) {
                                    continue;
                                }

                                var tex = hero.m_8203BFE1.FirstOrDefault(x => teResourceGUID.Index(x.m_id) == 0x40C9 || teResourceGUID.Index(x.m_id) == 0x40CA)?.m_texture;

                                if (tex == 0) {
                                    tex = hero.m_8203BFE1.FirstOrDefault()?.m_texture;
                                }

                                var image = new byte[] { };

                                var width = 128;
                                var height = 128;
                                
                                if (tex != 0) {
                                    teTexture texture = new teTexture(OpenFile(tex));
                                    if (texture.PayloadRequired) {
                                        ulong payload = texture.GetPayloadGUID(tex, 1);
                                        Stream payloadStream = OpenFile(payload);
                                        if (payloadStream != null) {
                                            texture.LoadPayload(payloadStream, 1);
                                        } else {
                                            continue;
                                        }
                                    }

                                    width = texture.Header.Width;
                                    height = texture.Header.Height;

                                    Stream ms = texture.SaveToDDS(1);

                                    image = DDSConverter.ConvertDDS(ms, DXGI_FORMAT.R8G8B8A8_UNORM, DDSConverter.ImageFormat.PNG, 0);
                                }

                                var entry = control.Add(heroNameActual, image, 128, (int)ImagingHelper.CalculateSizeAS(height, width, 128));
                                entry.Payload = progressionUnlocks;
                                entry.OnClick += (sender, args) => {
                                    window.Close();
                                };
                            } catch {
                                // ignored
                            } finally {
                                i += 1;
                                worker.ReportProgress((int) (i / (float) max * 100));
                            }
                        }

                        tcs.SetResult(control);
                    } catch (Exception e) {
                        tcs.SetException(e);
                    } finally {
                        worker.ReportProgress(100);
                    }
                });
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            }, new Tuple<ProgressWorker, TaskCompletionSource<Control>>(a1, source));
            return source.Task;
        }
    }
}
