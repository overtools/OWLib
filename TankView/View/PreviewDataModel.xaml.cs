using System;
using System.IO;
using System.Windows.Controls;
using TankLib;
using TankView.Helper;
using TankView.ViewModel;

using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace TankView.View {
    /// <summary>
    /// Interaction logic for PreviewDataModel.xaml
    /// </summary>
    public partial class PreviewDataModel : UserControl {
        public string GUIDString { get; set; }

        public Model3DGroup CurrentModel;

        public PreviewDataModel(GUIDEntry entry) {

            InitializeComponent();
            InitializeHelix();
            ParseModel(entry);
        }

        private void InitializeHelix() {

            //MyHelixViewport.Children.Add(new DefaultLights());
            //MyHelixViewport.IsHeadLightEnabled = false;
            AmbientLight ambLight = new AmbientLight(Color.FromRgb(80, 80, 80));

            var gLight = new DirectionalLight(Color.FromRgb(50, 50, 50), new Vector3D(0, 0, 0));


            gLight.Transform.Transform(MyHelixViewport.Camera.Position);

            MyHelixViewport.Lights.Children.Add(ambLight);
            MyHelixViewport.Lights.Children.Add(gLight);
        }

        private void ParseModel(GUIDEntry entry) {

            Vector3D xAxis = new Vector3D(1, 0, 0);
            Vector3D zAxis = new Vector3D(0, 0, 1);

            MyHelixViewport.Camera.Position = new Point3D(10, 20, 5);
            MyHelixViewport.Camera.LookDirection = new Vector3D(-10, -20, -5);
            


            // get the stream of the entry to convert
            using (Stream i = IOHelper.OpenFile(entry)) {

                var chunkedData = new teChunkedData(i);
                var rnd = new Random();
                foreach (var chunk in chunkedData.Chunks) {
                    if (chunk is TankLib.Chunks.teModelChunk_RenderMesh) {
                        var objFile = (chunk as TankLib.Chunks.teModelChunk_RenderMesh).ExportToObj();

                        if (File.Exists(objFile)) {

                            ObjReader reader = new ObjReader();
                            Model3DGroup objs = reader.Read(objFile);
                            Model3DGroup group = new Model3DGroup();
                            var r = (byte) rnd.Next(255);
                            var g = (byte) rnd.Next(255);
                            var b = (byte) rnd.Next(255);
                            foreach (var obj in objs.Children) {
                                var mdl = obj as GeometryModel3D;
                                var mat = MaterialHelper.CreateMaterial(Color.FromRgb((byte) 255, (byte) 128,(byte) 0));

                                //var mat = MaterialHelper.CreateMaterial(Color.FromRgb(r,g,b));
                                //var mat = new DiffuseMaterial(Brushes.Orange);
                                group.Children.Add(new GeometryModel3D() { Geometry = mdl.Geometry, Material = mat });
                            }


                            Matrix3D transformationMatrix = group.Transform.Value;
                            transformationMatrix.Rotate(new Quaternion(xAxis, 90));
                            transformationMatrix.Rotate(new Quaternion(zAxis, 180));

                            group.Transform = new MatrixTransform3D(transformationMatrix);

                            CurrentModel = group;
                            Content3D.Content = CurrentModel;
                        }


                    }
                }

            }

        }

        private void MyHelixViewport_CameraChanged(object sender, System.Windows.RoutedEventArgs e) {

        }
    }
}
