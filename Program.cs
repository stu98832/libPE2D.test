using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace libPE2D.test
{
    class Program
    {
        static void glCircle(double r)
        {
            int n = (int)(MathHelper.TwoPi * r+1.0);
            double theta = MathHelper.TwoPi / n;
            double c = Math.Cos(theta);
            double t = Math.Tan(theta);
            double x = r;
            double y = 0.0;

            GL.Begin(BeginMode.LineLoop);
            GL.Vertex2(0, 0);
            for (int i = 0; i < n; ++i)
            {
                GL.Vertex2(x, y);
                double tx = (-y * t);
                double ty = (x * t);
                x += tx;
                y += ty;
                x *= c;
                y *= c;
            }
            GL.End();
        }

        static Vector2[] shapeA = {
            new Vector2(-10, -10), new Vector2(10, -10),
            new Vector2(10, 10), new Vector2(-10, 10)
        };
        static Vector2[] shapeB = new Vector2[5];
        static void Main(string[] args)
        {
            for (int i = 0; i < 5; ++i)
                shapeB[i] = Vector2.Transform(new Vector2(40, 0), Quaternion.FromAxisAngle(Vector3.UnitZ, MathHelper.TwoPi * i / 5));

            GameWindow wnd = new GameWindow() { ClientSize = new Size(800, 600) };
            var start = DateTime.Now.Ticks;
            var end = DateTime.Now.Ticks;

            //physics engine
            float fps = 60.0f;
            Scene scene = new Scene(1/fps, 10, new Vector2(0, 98.0f));

            Random randomizer = new Random((int)DateTime.Now.Ticks);
            wnd.Mouse.ButtonDown += (s, e) =>
            {
                for (int t = 0; t < 1; ++t)
                {
                    if (e.Button == MouseButton.Left)
                    {
                        var body = scene.AddBody(new Circle(randomizer.Next(20, 80)), Material.Rock);
                        body.Transform.Position = new Vector2(e.X, e.Y);
                    }
                    else if (e.Button == MouseButton.Right)
                    {
                        int n = 3 + (int)(randomizer.NextDouble() * 40);
                        Vector2[] v = new Vector2[n];

                        for (int i = 0; i < n; ++i)
                        {
                            float k = 50 + (float)randomizer.NextDouble() * 50;

                            v[i] = new Vector2(
                                (float)(-k + randomizer.NextDouble() * 2 * k),
                                (float)(-k + randomizer.NextDouble() * 2 * k)
                            );
                        }

                        var body = scene.AddBody(new Polygon(v), Material.Rock);
                        body.Transform.Position = new Vector2(e.X, e.Y);
                        body.Transform.Orientation = (float)randomizer.NextDouble() * MathHelper.TwoPi;
                    }
                }
            };
            float accumulator = 0.0f;
            wnd.UpdateFrame += (s, e) =>
            {
                accumulator += (float)e.Time;
                if (accumulator > 1.0f)
                    accumulator = 1.0f;

                while (accumulator >= scene.UpdateTime)
                {
                    accumulator -= scene.UpdateTime;
                    scene.Step();
                }
            };
            wnd.RenderFrame += (s, e) =>
            {
                GL.ClearColor(0, 0, 0, 1.0f);
                GL.Clear(ClearBufferMask.ColorBufferBit);

                foreach (RigidBody obj in scene.Objects)
                    RanderBody(scene, obj);

                GL.MatrixMode(MatrixMode.Modelview);
                GL.LoadIdentity();

                GL.PointSize(4);
                GL.Color3(1.0, 0.0, 0.0);
                GL.Begin(BeginMode.Points);
                foreach (Manifold manifold in scene.Contacts)
                    foreach (Vector2 v in manifold.ContactPoints)
                        GL.Vertex2(v.X, v.Y);
                GL.End();
                GL.PointSize(1);
                
                GL.Color3(0.0, 1.0, 0.0);
                GL.Begin(BeginMode.Lines);
                foreach (Manifold manifold in scene.Contacts)
                {
                    Vector2 n = manifold.Normal;
                    foreach (Vector2 v in manifold.ContactPoints)
                    {
                        GL.Vertex2(v);
                        GL.Vertex2(v + 10*n);
                    }
                }
                GL.End();


                wnd.SwapBuffers();
            };

            var floor = scene.AddBody(new Polygon(-300, -10, 300, 10), Material.Metal);
            floor.SetStatic();
            floor.Transform.Position = new Vector2(400, 560);

            var objS = scene.AddBody(new Circle(30), Material.Wood);
            objS.SetStatic();
            objS.Transform.Position = new Vector2(440, 400);

            wnd.Visible = true;

            Matrix4 m = Matrix4.CreateOrthographicOffCenter(0, 800, 600, 0, 10.0f, 0.0f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref m);
            GL.Viewport(0, 0, 800, 600);

            wnd.Run(fps, fps);
        }

        static AABB worldAABB = new AABB(0, 0, 800, 600);
        static void RanderBody(Scene s, RigidBody body)
        {
            Matrix4 trans = Matrix4.CreateTranslation(new Vector3(body.Transform.Position));
            Matrix4 rotat =  Matrix4.CreateRotationZ(body.Transform.Orientation);
            Matrix4 m = rotat * trans;

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref m);

            AABB aabb = body.Shape.GetAABB();
            aabb.Offset(body.Transform.Position);

            if (!aabb.IsIntersection(worldAABB))
                return;

            if (body.Shape.Type == ShapeType.Circle)
            {
                Circle c = body.Shape as Circle;
                
                GL.Color3(1.0f, 1.0f, 1.0f);
                glCircle(c.Radius);
            }
            else if (body.Shape.Type == ShapeType.Polygon)
            {
                Polygon p = body.Shape as Polygon;
                
                GL.Color3(1.0f, 1.0f, 1.0f);
                GL.Begin(BeginMode.LineLoop);
                foreach (Vector2 v in p.Vertices)
                    GL.Vertex2(v);
                GL.End();
            }
            
        }
    }
}
