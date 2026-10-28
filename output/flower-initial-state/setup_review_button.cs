if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Edit Mode required.");
var preview=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerAppearancePreview>();
var row=UnityEngine.GameObject.Find("Appearance Controls").GetComponent<UnityEngine.RectTransform>();
var initial=row.Find("Initial State");
UnityEngine.UI.Button button;
if(initial==null)
{
    var template=row.Find("All Petals").gameObject;
    foreach(UnityEngine.RectTransform child in row)child.anchoredPosition+=new UnityEngine.Vector2(168,0);
    row.sizeDelta+=new UnityEngine.Vector2(168,0);
    var go=UnityEngine.Object.Instantiate(template,row);go.name="Initial State";
    var rect=go.GetComponent<UnityEngine.RectTransform>();rect.anchoredPosition=new UnityEngine.Vector2(0,0);rect.sizeDelta=new UnityEngine.Vector2(160,0);
    go.GetComponentInChildren<UnityEngine.UI.Text>().text="Initial state";
    button=go.GetComponent<UnityEngine.UI.Button>();
}
else button=initial.GetComponent<UnityEngine.UI.Button>();
while(button.onClick.GetPersistentEventCount()>0)UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick,0);
UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,preview.ShowInitialState);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(preview.gameObject.scene);
return new{button=button.name,action=button.onClick.GetPersistentMethodName(0)};
