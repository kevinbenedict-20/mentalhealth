using UnityEngine;

namespace MentalHealthApp.Generators
{
    public class ModernTableGenerator : MonoBehaviour
    {
        [Header("Table Dimensions")]
        public float tableDiameter = 2.4f;
        public float tableHeight = 0.74f;

        [HideInInspector]
        public GameObject tableObj;

        public void GenerateTable()
        {
            if (tableObj != null)
            {
                DestroyImmediate(tableObj);
            }

            tableObj = new GameObject("ModernRoundTable");
            tableObj.transform.SetParent(transform);
            tableObj.transform.position = Vector3.zero;

            // Warm natural wood material matching reference image
            Material roundTabletopMat = MaterialHelper.CreateMaterial("WarmWoodTabletopMat", new Color(0.72f, 0.52f, 0.35f), 0.1f, 0.65f);
            Material pedestalMat = MaterialHelper.CreateMaterial("TablePedestalMat", new Color(0.25f, 0.28f, 0.32f), 0.5f, 0.4f);

            // 1. Circular Tabletop Disc
            GameObject discTop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            discTop.name = "RoundTabletopSurface";
            discTop.transform.SetParent(tableObj.transform);
            discTop.transform.position = new Vector3(0, tableHeight - 0.04f, 0);
            discTop.transform.localScale = new Vector3(tableDiameter, 0.04f, tableDiameter);
            discTop.GetComponent<Renderer>().sharedMaterial = roundTabletopMat;

            // Bevel Edge Rim
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "TableRim";
            rim.transform.SetParent(tableObj.transform);
            rim.transform.position = new Vector3(0, tableHeight - 0.06f, 0);
            rim.transform.localScale = new Vector3(tableDiameter + 0.04f, 0.03f, tableDiameter + 0.04f);
            rim.GetComponent<Renderer>().sharedMaterial = roundTabletopMat;

            // 2. Central Pillar Pedestal Support
            GameObject centralLeg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            centralLeg.name = "CentralPedestalColumn";
            centralLeg.transform.SetParent(tableObj.transform);
            centralLeg.transform.position = new Vector3(0, tableHeight / 2f, 0);
            centralLeg.transform.localScale = new Vector3(0.22f, tableHeight / 2f, 0.22f);
            centralLeg.GetComponent<Renderer>().sharedMaterial = pedestalMat;

            // Floor Base Ring
            GameObject baseRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseRing.name = "FloorBaseRing";
            baseRing.transform.SetParent(tableObj.transform);
            baseRing.transform.position = new Vector3(0, 0.02f, 0);
            baseRing.transform.localScale = new Vector3(0.9f, 0.02f, 0.9f);
            baseRing.GetComponent<Renderer>().sharedMaterial = pedestalMat;
        }
    }
}
