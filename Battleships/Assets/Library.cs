using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Library
{
    public static class SmoothingFuncitons
    {
        public static float ApproachReferenceLinear(float input, float reference, float speed)
        {
            float direction = System.Math.Sign(reference - input);
            float output = input + direction * speed;
            output = direction > 0 ? System.Math.Min(output, reference) : System.Math.Max(output, reference);
            return output;
        }
        
        public static Vector3 ApproachReferenceLinear(Vector3 input, Vector3 reference, float speed)
        {
            return new Vector3 (ApproachReferenceLinear(input.x, reference.x, speed), ApproachReferenceLinear(input.y, reference.y, speed), ApproachReferenceLinear(input.z, reference.z, speed));
        }

        public static Vector3 ApproachReferenceLinear(Vector3 input, Vector3 reference, Vector3 speeds)
        {
            return new Vector3 (ApproachReferenceLinear(input.x, reference.x, speeds.x), ApproachReferenceLinear(input.y, reference.y, speeds.y), ApproachReferenceLinear(input.z, reference.z, speeds.z));
        }

        public static float Damp(float a, float b, float lambda, float dt)
        {
            return Mathf.Lerp(a, b, 1 - Mathf.Exp(-lambda * dt));
        }
        
        public static Vector3 Damp(Vector3 a, Vector3 b, float lambda, float dt)
        {
            return new Vector3(Damp(a.x, b.x, lambda, dt), Damp(a.y, b.y, lambda, dt), Damp(a.z, b.z, lambda, dt));
        }
        
        public static Vector3 Damp(Vector3 a, Vector3 b, Vector3 lambdas, float dt)
        {
            return new Vector3(Damp(a.x, b.x, lambdas.x, dt), Damp(a.y, b.y, lambdas.y, dt), Damp(a.z, b.z, lambdas.z, dt));
        }

        /// <summary>
        /// Based on magnitude
        /// </summary>
        /// <param name="input"> value to lerp</param>
        /// <param name="reference">target value</param>
        /// <param name="riseLerpValue">lag when rising</param>
        /// <param name="fallLerpValue">lag when falling</param>
        /// <returns></returns>
        public static UnityEngine.Vector2 LerpToReferenceNonLinear(UnityEngine.Vector2 input, UnityEngine.Vector2 reference, float riseLerpValue, float fallLerpValue)
        {
            UnityEngine.Vector2 output;
            if (UnityEngine.Vector2.SqrMagnitude(reference) > UnityEngine.Vector2.SqrMagnitude(input))
            {
                output = UnityEngine.Vector2.Lerp(input, reference, riseLerpValue);
            }
            else
            {
                output = UnityEngine.Vector2.Lerp(input, reference, fallLerpValue);
            }

            return output;
        }

        public static UnityEngine.Vector2 ApproachReferenceNonLinear(UnityEngine.Vector2 input, UnityEngine.Vector2 reference, UnityEngine.Vector2 speed, float riseLerpValue, float fallLerpValue)
        {
            UnityEngine.Vector2 output;
            if (UnityEngine.Vector2.SqrMagnitude(reference) > UnityEngine.Vector2.SqrMagnitude(input))
            {
                output = UnityEngine.Vector2.Lerp(input, reference, riseLerpValue);
            }
            else
            {
                output = UnityEngine.Vector2.Lerp(input, reference, fallLerpValue);
            }

            return output;
        }
    }

    public static class Helpers
    {
        public static float MinValue(float signedInput, float unsignedReference)
        {
            return Mathf.Sign(signedInput) * Mathf.Min(Mathf.Abs(signedInput), unsignedReference);
        }

        public static float GetDeltaYToScreenBottom(UnityEngine.Vector3 objectPos, UnityEngine.Vector3 currentCamPos, float vertFOV)
        {
            float distanceFromCamera = System.Math.Abs(objectPos.z - currentCamPos.z);
            float deltaObjY = distanceFromCamera * (float)System.Math.Tan(vertFOV * 0.5 * (System.Math.PI / 180.0));

            float deltaObjYtoCam0 = currentCamPos.y - objectPos.y;

            Debug.Log("deltaObjYtoCam0: " + deltaObjYtoCam0);
            Debug.Log("deltaObjYtoScreenBottom: " + deltaObjY);
            return deltaObjYtoCam0 - deltaObjY;
        }

        public static void Draw3DCross(Vector3 center, float size)
        {
            Vector3 start, end;
            start = center;
            end = center;
            start.x -= size;
            end.x += size;
            Debug.DrawLine(start, end);
            start = center;
            end = center;
            start.y -= size;
            end.y += size;
            Debug.DrawLine(start, end);
            start = center;
            end = center;
            start.z -= size;
            end.z += size;
            Debug.DrawLine(start, end);
        }
    }

    public static class TextReadWriter
   {
        public static void WriteTextToFile(string content, string resourceFolder, string fileNameNoExt)
        {
            string wholePath = "Assets/Resources/" + resourceFolder + fileNameNoExt + ".txt";
            System.IO.StreamWriter writer = new System.IO.StreamWriter(wholePath, false);
            writer.WriteLine(content);
            writer.Close();

#if UNITY_EDITOR
            //Re-import the file to update the reference in the editor
            UnityEditor.AssetDatabase.ImportAsset(wholePath);
#endif

            //Print the text from the file
            UnityEngine.TextAsset asset = (UnityEngine.TextAsset) UnityEngine.Resources.Load(resourceFolder + fileNameNoExt);
            UnityEngine.Debug.Log(asset.text);
        }
        public static string LoadTextFromFile(string fileResourcePath, string fileNameNoExt)
        {
            string content = "";

            UnityEngine.TextAsset asset = (UnityEngine.TextAsset) UnityEngine.Resources.Load("Assets/Resources/" + fileResourcePath + fileNameNoExt);
            content = asset.text;
            return content;
        }

        public static void SaveVec3(UnityEngine.Vector3 serializable, string folderPath, string fileNameNoExt)
        {
            string jsonText = UnityEngine.JsonUtility.ToJson(serializable);

            WriteTextToFile(jsonText, folderPath, fileNameNoExt);
        }

        public static UnityEngine.Vector3 LoadVec3(string folderPath, string fileNameNoExt)
        {
            string jsonText = LoadTextFromFile(folderPath, fileNameNoExt);

            UnityEngine.Vector3 vector3 = UnityEngine.JsonUtility.FromJson<UnityEngine.Vector3>(jsonText);

            UnityEngine.Debug.Log("Loaded " + vector3.ToString());
            return vector3;
        }
    }



    public static class TextureWriter
    {
        public enum SaveTextureFileFormat
        {
            EXR, JPG, PNG, TGA
        };

        /// <summary>
        /// Saves a Texture2D to disk with the specified filename and image format
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="filePath"></param>
        /// <param name="fileFormat"></param>
        /// <param name="jpgQuality"></param>
        static public void SaveTexture2DToFile(UnityEngine.Texture2D tex, string filePath, SaveTextureFileFormat fileFormat, int jpgQuality = 95)
        {
            switch (fileFormat)
            {
                case SaveTextureFileFormat.EXR:
                    System.IO.File.WriteAllBytes(filePath + ".exr", tex.EncodeToEXR());
                    break;
                case SaveTextureFileFormat.JPG:
                    System.IO.File.WriteAllBytes(filePath + ".jpg", tex.EncodeToJPG(jpgQuality));
                    break;
                case SaveTextureFileFormat.PNG:
                    System.IO.File.WriteAllBytes(filePath + ".png", tex.EncodeToPNG());
                    break;
                case SaveTextureFileFormat.TGA:
                    System.IO.File.WriteAllBytes(filePath + ".tga", tex.EncodeToTGA());
                    break;
            }
        }


        /// <summary>
        /// Saves a RenderTexture to disk with the specified filename and image format
        /// </summary>
        /// <param name="renderTexture"></param>
        /// <param name="filePath"></param>
        /// <param name="fileFormat"></param>
        /// <param name="jpgQuality"></param>
        static public void SaveRenderTextureToFile(RenderTexture renderTexture, string fileName, bool linearColorSpace, SaveTextureFileFormat fileFormat = SaveTextureFileFormat.PNG, int jpgQuality = 95)
        {
            string filePath = "Assets/Generated/" + fileName;

            Texture2D tex;
            if (fileFormat != SaveTextureFileFormat.EXR)
                tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false, linearColorSpace);
            else
                tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBAFloat, false, linearColorSpace);

           // tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false, true);

            var oldRt = RenderTexture.active;
            RenderTexture.active = renderTexture;
            tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            tex.Apply();
            RenderTexture.active = oldRt;
            SaveTexture2DToFile(tex, filePath, fileFormat, jpgQuality);

            /*if (Application.isPlaying)
                Object.Destroy(tex);
            else
                Object.DestroyImmediate(tex);*/

        }

    }
}