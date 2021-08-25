using UnityEngine;

public class SpawnGhost : MonoBehaviour
{

    private Transform visual;
    private ScriptableObjectType placedObjectTypeSO;

    private void Start()
    {
        RefreshVisual();

        GridBuildingSystem.Instance.OnSelectedChanged += Instance_OnSelectedChanged;  //lancia evento se cambia prefab selezionato -> distruggi, ricrea
    }

    private void Instance_OnSelectedChanged(object sender, System.EventArgs e)  //aggiorna visual del prefab selezionato
    {
        RefreshVisual();
    }

    private void LateUpdate()
    {
        Vector3 targetPosition = GridBuildingSystem.Instance.GetMouseWorldSnappedPosition();  //converti mouseworldposition in gridposition
        targetPosition.y = 2f;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 15f); //usa lerp per animazione

        transform.rotation = Quaternion.Lerp(transform.rotation, GridBuildingSystem.Instance.GetPlacedObjectRotation(), Time.deltaTime * 15f); //dopo position prendo la rotation
    }

    private void RefreshVisual()
    {
        if (visual != null)
        {
            Destroy(visual.gameObject);
            visual = null;
        }

        ScriptableObjectType placedObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSO();

        if (placedObjectTypeSO != null)
        {
            visual = Instantiate(placedObjectTypeSO.visual, Vector3.zero, Quaternion.identity);
            visual.parent = transform;
            visual.localPosition = Vector3.zero;
            visual.localEulerAngles = Vector3.zero;
            SetLayerRecursive(visual.gameObject, 10); //layer ghost di buildghost
        }
    }

    private void SetLayerRecursive(GameObject targetGameObject, int layer)
    {
        targetGameObject.layer = layer;
        foreach (Transform child in targetGameObject.transform)
        {
            SetLayerRecursive(child.gameObject, layer); //setto il layer ghost a tutte le visual
        }
    }

}
