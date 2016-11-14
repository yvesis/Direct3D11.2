using LoadMeshes.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LoadMeshes
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var form = new Form1();
            form.Text = "D3DRendering - Primitives";
            form.ClientSize = new System.Drawing.Size(640, 480);
            form.Show();

            using (var app3D = new D3DApp(form))
            {
                // render frames at max rate

                app3D.VSync = true;

                //Initialize framework

                app3D.Initialize();

                // Render loop

                app3D.Run();
            }
        }
    }
}
