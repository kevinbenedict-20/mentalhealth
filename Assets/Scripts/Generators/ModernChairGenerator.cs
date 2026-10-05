using UnityEngine;
using System.Collections.Generic;

namespace MentalHealthApp.Generators
{
    public class ModernChairGenerator : MonoBehaviour
    {
        [HideInInspector]
        public List<GameObject> generatedChairs = new List<GameObject>();

        [HideInInspector]
        public List<Transform> seatAnchors = new List<Transform>();

        public void GenerateChairs(float tableDiameter, float tableHeight)
        {
            foreach (GameObject c in generatedChairs)
            {
                if (c != null) DestroyImmediate(c);
            }
            generatedChairs.Clear();
            seatAnchors.Clear();

            GameObject chairsGroup = new GameObject("ModernChairs");
            chairsGroup.transform.SetParent(transform);

            // Materials matching reference image (Dark slate grey chair cushion + matte frame)
            Material darkSlateMat = MaterialHelper.CreateMaterial("DarkSlateChairMat", new Color(0.22f, 0.25f, 0.28f), 0.1f, 0.4f);
            Material chairLegMat = MaterialHelper.CreateMaterial("ChairLegMat", new Color(0.12f, 0.14f, 0.16f), 0.5f, 0.3f);

            float radius = (tableDiameter / 2f) + 0.45f;
            int count = 5;

            // Specific angles around round table matching reference image
            // Seat 0: Center Back (Moderator - Purple Hoodie) facing foreground (180 deg)
            // Seat 1: Foreground Left (Striped Sweater) facing inside (35 deg)
            // Seat 2: Mid Left (Green Hoodie) facing inside (85 deg)
            // Seat 3: Mid Right (Dark Blue Hoodie) facing inside (-85 deg)
            // Seat 4: Foreground Right (Burgundy Sweater) facing inside (-35 deg)

            float[] angles = new float[] { 180f, 35f, 85f, -85f, -35f };

            for (int i = 0; i < count; i++)
            {
                float angleRad = angles[i] * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Sin(angleRad) * radius, 0, Mathf.Cos(angleRad) * radius);
                Quaternion rot = Quaternion.Euler(0, angles[i] + 180f, 0); // Face table center

                GameObject chair = BuildSlateErgonomicChair("ErgonomicChair_" + i, pos, rot, darkSlateMat, chairLegMat);
                chair.transform.SetParent(chairsGroup.transform);
                generatedChairs.Add(chair);

                Transform seatAnchor = chair.transform.Find("SeatAnchor");
                if (seatAnchor != null) seatAnchors.Add(seatAnchor);
            }
        }

        private GameObject BuildSlateErgonomicChair(string name, Vector3 pos, Quaternion rot, Material cushionMat, Material legMat)
        {
            GameObject chair = new GameObject(name);
            chair.transform.position = pos;
            chair.transform.rotation = rot;

            float seatHeight = 0.48f;

            // 4 Legs
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    leg.name = "ChairLeg";
                    leg.transform.SetParent(chair.transform);
                    leg.transform.localPosition = new Vector3(x * 0.18f, seatHeight / 2f, z * 0.18f);
                    leg.transform.localScale = new Vector3(0.04f, seatHeight / 2f, 0.04f);
                    leg.GetComponent<Renderer>().sharedMaterial = legMat;
                }
            }

            // Seat Cushion
            GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.name = "SeatCushion";
            seat.transform.SetParent(chair.transform);
            seat.transform.localPosition = new Vector3(0, seatHeight, 0);
            seat.transform.localScale = new Vector3(0.46f, 0.06f, 0.46f);
            seat.GetComponent<Renderer>().sharedMaterial = cushionMat;

            // Seat Anchor
            GameObject seatAnchor = new GameObject("SeatAnchor");
            seatAnchor.transform.SetParent(chair.transform);
            seatAnchor.transform.localPosition = new Vector3(0, seatHeight + 0.04f, -0.05f);
            seatAnchor.transform.localRotation = Quaternion.identity;

            // Curved Backrest
            GameObject backrest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backrest.name = "ErgonomicBackrest";
            backrest.transform.SetParent(chair.transform);
            backrest.transform.localPosition = new Vector3(0, seatHeight + 0.32f, -0.2f);
            backrest.transform.localRotation = Quaternion.Euler(-8f, 0, 0);
            backrest.transform.localScale = new Vector3(0.44f, 0.55f, 0.05f);
            backrest.GetComponent<Renderer>().sharedMaterial = cushionMat;

            return chair;
        }
    }
}
