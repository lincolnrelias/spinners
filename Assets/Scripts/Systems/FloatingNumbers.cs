using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FloatingNumbers : MonoBehaviour
{
    public static FloatingNumbers Instance;

    const int PoolSize = 12;
    readonly List<TMP_Text> _pool = new(PoolSize);

    void Awake()
    {
        Instance = this;
        for (int i = 0; i < PoolSize; i++)
        {
            var go  = new GameObject($"FN_{i}");
            go.transform.SetParent(transform, false);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.alignment    = TextAlignmentOptions.Center;
            tmp.fontSize     = 14f;
            tmp.sortingOrder = 20;
            go.SetActive(false);
            _pool.Add(tmp);
        }
    }

    public void Show(float x, float y, string text, Color color, float fontSize = 14f)
    {
        TMP_Text slot = null;
        for (int i = 0; i < _pool.Count; i++)
            if (!_pool[i].gameObject.activeSelf) { slot = _pool[i]; break; }
        if (slot == null) return;

        slot.text     = text;
        slot.color    = color;
        slot.fontSize = fontSize;
        slot.transform.position = new Vector3(x, y, -1f);
        slot.gameObject.SetActive(true);
        StartCoroutine(FloatUp(slot));
    }

    IEnumerator FloatUp(TMP_Text label)
    {
        float elapsed = 0f;
        const float dur = 1.2f;
        Vector3 start   = label.transform.position;
        Color baseColor = label.color;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            label.transform.position = start + new Vector3(0f, 50f * t, 0f);
            Color c = baseColor; c.a = 1f - t;
            label.color = c;
            yield return null;
        }
        label.gameObject.SetActive(false);
    }
}
