using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace GerardoUtils
{
    public static class UtilsClass
    {
        public const int sortingOrderDefault = 5000;


        // Create Text in the World
        public static TextMesh CreateWorldText(string text, Transform parent = null, Vector3 localPosition = default(Vector3), int fontSize = 40, Color? color = null, TextAnchor textAnchor = TextAnchor.UpperLeft, TextAlignment textAlignment = TextAlignment.Left, int sortingOrder = sortingOrderDefault)
        {
            if (color == null) color = Color.white;
            return CreateWorldText(parent, text, localPosition, fontSize, (Color)color, textAnchor, textAlignment, sortingOrder);
        }

        // Create Text in the World
        public static TextMesh CreateWorldText(Transform parent, string text, Vector3 localPosition, int fontSize, Color color, TextAnchor textAnchor, TextAlignment textAlignment, int sortingOrder)
        {
            GameObject gameObject = new GameObject("World_Text", typeof(TextMesh));
            Transform transform = gameObject.transform;
            transform.SetParent(parent, false);
            transform.localPosition = localPosition;
            TextMesh textMesh = gameObject.GetComponent<TextMesh>();
            textMesh.anchor = textAnchor;
            textMesh.alignment = textAlignment;
            textMesh.text = text;
            textMesh.fontSize = fontSize;
            textMesh.color = color;
            textMesh.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
            return textMesh;
        }


        // Get Mouse Position in World with Z = 0f //IN CASO DI SCENA 2D
        public static Vector3 GetMouseWorldPosition()
        {
            Vector3 vec = GetMouseWorldPositionWithZ(Input.mousePosition, Camera.main);
            vec.z = 0f;
            return vec;
        }

        public static Vector3 GetMouseWorldPositionWithZ()
        {
            return GetMouseWorldPositionWithZ(Input.mousePosition, Camera.main);
        }
        public static Vector3 GetMouseWorldPositionWithZ(Camera worldCamera)
        {
            return GetMouseWorldPositionWithZ(Input.mousePosition, worldCamera);
        }
        public static Vector3 GetMouseWorldPositionWithZ(Vector3 screenPosition, Camera worldCamera)
        {
            Vector3 worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
            return worldPosition;
        }


        // Create a Text Popup in the World, no parent
        public static void CreateWorldTextPopup(string text, Vector3 localPosition, int fontSize, Color color)
        {
            CreateWorldTextPopup(null, text, localPosition, fontSize, color, localPosition + new Vector3(0, 10), 2f);
        }

        // Create a Text Popup in the World
        public static void CreateWorldTextPopup(Transform parent, string text, Vector3 localPosition, int fontSize, Color color, Vector3 finalPopupPosition, float popupTime)
        {
            TextMesh textMesh = CreateWorldText(parent, text, localPosition, fontSize, color, TextAnchor.LowerLeft, TextAlignment.Left, sortingOrderDefault);
            Transform transform = textMesh.transform;
            Vector3 moveAmount = (finalPopupPosition - localPosition) / popupTime;
            FunctionUpdater.Create(delegate ()
            {
                transform.position += moveAmount * Time.deltaTime;
                popupTime -= Time.deltaTime;
                if (popupTime <= 0f)
                {
                    UnityEngine.Object.Destroy(transform.gameObject);
                    return true;
                }
                else
                {
                    return false;
                }
            }, "WorldTextPopup");
        }



        // FunctionUpdater CLASS
        public class FunctionUpdater
        {
            /*
             * Class to hook Actions into MonoBehaviour
             * */
            private class MonoBehaviourHook : MonoBehaviour
            {

                public Action OnUpdate;

                private void Update()
                {
                    if (OnUpdate != null) OnUpdate();
                }

            }

            private static List<FunctionUpdater> updaterList; // Holds a reference to all active updaters
            private static GameObject initGameObject; // Global game object used for initializing class, is destroyed on scene change

            private static void InitIfNeeded()
            {
                if (initGameObject == null)
                {
                    initGameObject = new GameObject("FunctionUpdater_Global");
                    updaterList = new List<FunctionUpdater>();
                }
            }


            public static FunctionUpdater Create(Action updateFunc)
            {
                return Create(() => { updateFunc(); return false; }, "", true, false);
            }
            public static FunctionUpdater Create(Func<bool> updateFunc)
            {
                return Create(updateFunc, "", true, false);
            }
            public static FunctionUpdater Create(Func<bool> updateFunc, string functionName)
            {
                return Create(updateFunc, functionName, true, false);
            }
            public static FunctionUpdater Create(Func<bool> updateFunc, string functionName, bool active)
            {
                return Create(updateFunc, functionName, active, false);
            }
            public static FunctionUpdater Create(Func<bool> updateFunc, string functionName, bool active, bool stopAllWithSameName)
            {
                InitIfNeeded();

                if (stopAllWithSameName)
                {
                    StopAllUpdatersWithName(functionName);
                }

                GameObject gameObject = new GameObject("FunctionUpdater Object " + functionName, typeof(MonoBehaviourHook));
                FunctionUpdater functionUpdater = new FunctionUpdater(gameObject, updateFunc, functionName, active);
                gameObject.GetComponent<MonoBehaviourHook>().OnUpdate = functionUpdater.Update;

                updaterList.Add(functionUpdater);
                return functionUpdater;
            }
            private static void RemoveUpdater(FunctionUpdater funcUpdater)
            {
                InitIfNeeded();
                updaterList.Remove(funcUpdater);
            }
            public static void DestroyUpdater(FunctionUpdater funcUpdater)
            {
                InitIfNeeded();
                if (funcUpdater != null)
                {
                    funcUpdater.DestroySelf();
                }
            }
            public static void StopUpdaterWithName(string functionName)
            {
                InitIfNeeded();
                for (int i = 0; i < updaterList.Count; i++)
                {
                    if (updaterList[i].functionName == functionName)
                    {
                        updaterList[i].DestroySelf();
                        return;
                    }
                }
            }
            public static void StopAllUpdatersWithName(string functionName)
            {
                InitIfNeeded();
                for (int i = 0; i < updaterList.Count; i++)
                {
                    if (updaterList[i].functionName == functionName)
                    {
                        updaterList[i].DestroySelf();
                        i--;
                    }
                }
            }

            private GameObject gameObject;
            private string functionName;
            private bool active;
            private Func<bool> updateFunc; // Destroy Updater if return true;

            public FunctionUpdater(GameObject gameObject, Func<bool> updateFunc, string functionName, bool active)
            {
                this.gameObject = gameObject;
                this.updateFunc = updateFunc;
                this.functionName = functionName;
                this.active = active;
            }
            public void Pause()
            {
                active = false;
            }
            public void Resume()
            {
                active = true;
            }

            private void Update()
            {
                if (!active) return;
                if (updateFunc())
                {
                    DestroySelf();
                }
            }
            public void DestroySelf()
            {
                RemoveUpdater(this);
                if (gameObject != null)
                {
                    UnityEngine.Object.Destroy(gameObject);
                }
            }
        }

        //CameraTarget
        public static Vector3 ApplyRotationToVector(Vector3 vec, Vector3 vecRotation)
        {
            return ApplyRotationToVector(vec, GetAngleFromVectorFloat(vecRotation));
        }

        public static Vector3 ApplyRotationToVector(Vector3 vec, float angle)
        {
            return Quaternion.Euler(0, 0, angle) * vec;
        }

        public static Vector3 ApplyRotationToVectorXZ(Vector3 vec, float angle)
        {
            return Quaternion.Euler(0, angle, 0) * vec;
        }

        public static float GetAngleFromVectorFloat(Vector3 dir)
        {
            dir = dir.normalized;
            float n = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (n < 0) n += 360;

            return n;
        }

        public static float GetAngleFromVectorFloat3D(Vector3 dir)
        {
            dir = dir.normalized;
            float n = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
            if (n < 0) n += 360;

            return n;
        }
    }
}

