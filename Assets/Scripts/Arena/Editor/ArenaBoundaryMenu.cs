using UnityEditor;
using UnityEngine;

static class ArenaBoundaryMenu
{
    const int MenuPriority = 10;

    [MenuItem("GameObject/3D Object/Arena Boundary", false, MenuPriority)]
    static void CreateArenaBoundary(MenuCommand command)
    {
        GameObject go = new GameObject("Arena Boundary");
        Undo.RegisterCreatedObjectUndo(go, "Create Arena Boundary");
        go.AddComponent<ArenaBoundary>();
        GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
        Selection.activeGameObject = go;
    }

    [MenuItem("GameObject/3D Object/Arena Boundary", true)]
    static bool ValidateCreateArenaBoundary() => true;
}
