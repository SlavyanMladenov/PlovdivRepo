using UnityEngine;
using System.Collections;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;

namespace Dreamteck.Splines
{
    [System.Serializable]
    public struct S_Vector3
    {
        public float x, y, z;
        public Vector3 vector
        {
            get { return new Vector3(x, y, z); }
            set { }
        }


        public S_Vector3(Vector3 input)
        {
            x = input.x;
            y = input.y;
            z = input.z;
        }
    }
    [System.Serializable]
    public struct S_Color
    {
        public float r, g, b, a;
        public Color color
        {
            get { return new Color(r, g, b, a); }
            set { }
        }
        public S_Color(Color input)
        {
            r = input.r;
            g = input.g;
            b = input.b;
            a = input.a;
        }
    }

    [System.Serializable]
    public class SplinePreset : SplinePrimitive
    {
        private S_Vector3[] points_position = new S_Vector3[0];
        private S_Vector3[] points_tanget = new S_Vector3[0];
        private S_Vector3[] points_tangent2 = new S_Vector3[0];
        private S_Vector3[] points_normal = new S_Vector3[0];
        private S_Color[] points_color = new S_Color[0];
        private float[] points_size = new float[0];
        private SplinePoint.Type[] points_type = new SplinePoint.Type[0];

        public bool isClosed = false;
        public string filename = "";
        public string name = "";
        public string description = "";
        public Spline.Type type = Spline.Type.Bezier;
        private static string path = "";

        public SplinePoint[] points
        {
            get
            {
                SplinePoint[] p = new SplinePoint[points_position.Length];
                for(int i = 0; i < p.Length; i++)
                {
                    p[i].type = points_type[i];
                    p[i].position = points_position[i].vector;
                    p[i].tangent = points_tanget[i].vector;
                    p[i].tangent2 = points_tangent2[i].vector;
                    p[i].normal = points_normal[i].vector;
                    p[i].color = points_color[i].color;
                    p[i].size = points_size[i];
                }
                return p;
            }
        }

        public void Cancel()
        {
            Revert();
        }

        public SplinePreset (SplinePoint[] p, bool closed, Spline.Type t)
        {
            points_position = new S_Vector3[p.Length];
            points_tanget = new S_Vector3[p.Length];
            points_tangent2 = new S_Vector3[p.Length];
            points_normal = new S_Vector3[p.Length];
            points_color = new S_Color[p.Length];
            points_size = new float[p.Length];
            points_type = new SplinePoint.Type[p.Length];
            for(int i = 0; i < p.Length; i++)
            {
                points_position[i] = new S_Vector3(p[i].position);
                points_tanget[i] = new S_Vector3(p[i].tangent);
                points_tangent2[i] = new S_Vector3(p[i].tangent2);
                points_normal[i] = new S_Vector3(p[i].normal);
                points_color[i] = new S_Color(p[i].color);
                points_size[i] = p[i].size;
                points_type[i] = p[i].type;
            }
            isClosed = closed;
            type = t;
            path = FindFolder(Application.dataPath, "Dreamteck/Splines/Presets");
        }

        public void Save(string name)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream file = File.Create(path + "/" + name + ".dsp");
            formatter.Serialize(file, this);
            file.Close();
        }

        public static void Delete(string filename)
        {
            path = FindFolder(Application.dataPath, "Dreamteck/Splines/Presets");
            if (!Directory.Exists(path))
            {
                Debug.LogError("Directory " + path + " does not exist");
                return;
            }
            File.Delete(path + "/" + filename);
        }

        public static SplinePreset[] LoadAll()
        {
            path = FindFolder(Application.dataPath, "Dreamteck/Splines/Presets");
            if (!Directory.Exists(path))
            {
                Debug.LogError("Directory " + path + " does not exist");
                return null;
            }
            string[] files = System.IO.Directory.GetFiles(path, "*.dsp");
            SplinePreset[] presets = new SplinePreset[files.Length];
            for(int i = 0; i < files.Length; i++)
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(files[i], FileMode.Open);
                presets[i] = (SplinePreset)bf.Deserialize(file);
                presets[i].filename = new FileInfo(files[i]).Name;
                file.Close();
            }
            return presets;
        }

        private static string FindFolder(string dir, string folderPattern)
        {
            string[] folders = folderPattern.Split('/');
            int folderIndex = 0; 
            string foundDir = dir;
            try
            {
                foreach (string d in Directory.GetDirectories(dir))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(d);
                    if (folderIndex >= folders.Length) break;
                    if (dirInfo.Name == folders[folderIndex])
                    {
                        folderIndex++;
                        foundDir = d;
                        string[] fs = new string[folders.Length - folderIndex];
                        for (int i = 0; i < fs.Length; i++)
                        {
                            fs[i] = folders[i + folderIndex];
                        }
                        foundDir = FindFolder(d, string.Join("/", fs));
                    }
                }
            }
            catch (System.Exception excpt)
            {
                Debug.Log(excpt.Message);
            }
            return foundDir;
        }
    }
}
