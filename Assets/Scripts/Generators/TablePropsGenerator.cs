using UnityEngine;
using System.Collections.Generic;

namespace MentalHealthApp.Generators
{
    public class TablePropsGenerator : MonoBehaviour
    {
        public void GenerateProps(List<Transform> seatAnchors, float tableHeight)
        {
            GameObject propsGroup = new GameObject("TableProps");
            propsGroup.transform.SetParent(transform);

            Material whitePotMat = MaterialHelper.CreateMaterial("WhitePotMat", new Color(0.96f, 0.96f, 0.98f), 0.05f, 0.9f);
            Material leafMat = MaterialHelper.CreateMaterial("GreenPlantLeafMat", new Color(0.2f, 0.55f, 0.25f), 0.05f, 0.6f);
            Material laptopBodyMat = MaterialHelper.CreateMaterial("DarkLaptopMat", new Color(0.12f, 0.12f, 0.14f), 0.8f, 0.7f);
            Material notebookMat = MaterialHelper.CreateMaterial("NotebookPaperMat", new Color(0.96f, 0.96f, 0.92f), 0.0f, 0.2f);
            Material thermosMat = MaterialHelper.CreateMaterial("ThermosMat", new Color(0.18f, 0.28f, 0.38f), 0.7f, 0.5f);
            Material phoneMat = MaterialHelper.CreateMaterial("PhoneMat", new Color(0.08f, 0.08f, 0.1f), 0.8f, 0.8f);

            // 1. Central Potted Plant (Exact match for center of round table in reference image)
            GameObject plantPot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plantPot.name = "CenterPottedPlant";
            plantPot.transform.SetParent(propsGroup.transform);
            plantPot.transform.position = new Vector3(0, tableHeight + 0.08f, 0);
            plantPot.transform.localScale = new Vector3(0.25f, 0.08f, 0.25f);
            plantPot.GetComponent<Renderer>().sharedMaterial = whitePotMat;

            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f;
                Quaternion rot = Quaternion.Euler(20f, angle, 0);

                GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.transform.SetParent(plantPot.transform);
                stem.transform.localPosition = new Vector3(0, 0.8f, 0);
                stem.transform.rotation = rot;
                stem.transform.localScale = new Vector3(0.06f, 0.6f, 0.06f);
                stem.GetComponent<Renderer>().sharedMaterial = leafMat;

                GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaf.transform.SetParent(stem.transform);
                leaf.transform.localPosition = new Vector3(0, 0.85f, 0);
                leaf.transform.localScale = new Vector3(2.5f, 0.25f, 5f);
                leaf.GetComponent<Renderer>().sharedMaterial = leafMat;
            }

            // 2. Open Laptop near Seat 4 (Foreground Right)
            GameObject laptop = new GameObject("TableLaptop");
            laptop.transform.SetParent(propsGroup.transform);
            laptop.transform.position = new Vector3(0.35f, tableHeight, 0.3f);
            laptop.transform.rotation = Quaternion.Euler(0, -45f, 0);

            GameObject laptopBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            laptopBase.transform.SetParent(laptop.transform);
            laptopBase.transform.localPosition = new Vector3(0, 0.006f, 0);
            laptopBase.transform.localScale = new Vector3(0.32f, 0.012f, 0.22f);
            laptopBase.GetComponent<Renderer>().sharedMaterial = laptopBodyMat;

            GameObject laptopLid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            laptopLid.transform.SetParent(laptop.transform);
            laptopLid.transform.localPosition = new Vector3(0, 0.1f, -0.1f);
            laptopLid.transform.localRotation = Quaternion.Euler(-110f, 0, 0);
            laptopLid.transform.localScale = new Vector3(0.32f, 0.008f, 0.22f);
            laptopLid.GetComponent<Renderer>().sharedMaterial = laptopBodyMat;

            // 3. Open Notebooks with Pens near Seat 1 & 2
            BuildNotebook("Notebook_Seat1", new Vector3(-0.45f, tableHeight + 0.005f, -0.2f), Quaternion.Euler(0, 25f, 0), propsGroup.transform, notebookMat);
            BuildNotebook("Notebook_Seat2", new Vector3(-0.55f, tableHeight + 0.005f, 0.35f), Quaternion.Euler(0, -15f, 0), propsGroup.transform, notebookMat);

            // 4. Smartphone lying flat
            GameObject phone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            phone.name = "Smartphone";
            phone.transform.SetParent(propsGroup.transform);
            phone.transform.position = new Vector3(-0.25f, tableHeight + 0.005f, 0.15f);
            phone.transform.rotation = Quaternion.Euler(0, 65f, 0);
            phone.transform.localScale = new Vector3(0.08f, 0.006f, 0.15f);
            phone.GetComponent<Renderer>().sharedMaterial = phoneMat;

            // 5. Thermos Water Bottle near Seat 3
            GameObject thermos = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            thermos.name = "ThermosCup";
            thermos.transform.SetParent(propsGroup.transform);
            thermos.transform.position = new Vector3(0.55f, tableHeight + 0.12f, 0.1f);
            thermos.transform.localScale = new Vector3(0.08f, 0.12f, 0.08f);
            thermos.GetComponent<Renderer>().sharedMaterial = thermosMat;
        }

        private void BuildNotebook(string name, Vector3 pos, Quaternion rot, Transform parent, Material paperMat)
        {
            GameObject nb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nb.name = name;
            nb.transform.SetParent(parent);
            nb.transform.position = pos;
            nb.transform.rotation = rot;
            nb.transform.localScale = new Vector3(0.24f, 0.008f, 0.32f);
            nb.GetComponent<Renderer>().sharedMaterial = paperMat;

            GameObject pen = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pen.name = "Pen";
            pen.transform.SetParent(nb.transform);
            pen.transform.localPosition = new Vector3(0.4f, 0.6f, 0);
            pen.transform.localRotation = Quaternion.Euler(0, 0, 90);
            pen.transform.localScale = new Vector3(0.04f, 0.4f, 0.04f);
            pen.GetComponent<Renderer>().sharedMaterial = MaterialHelper.CreateMaterial("PenMat", new Color(0.1f, 0.1f, 0.12f));
        }
    }
}
