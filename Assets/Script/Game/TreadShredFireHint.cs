using TreadShred.Progression;
using UnityEngine;
using UnityEngine.UI;

/// <summary>D-035-AC-03: TAP HERE TO FIRE above the nearest enemy, until the player's first shot.</summary>
public sealed class TreadShredFireHint : MonoBehaviour
{
    private static readonly string[] EnemyTags = { "targetMask", "emnemyred", "emnemyorange", "emnemyblack" };

    private RectTransform root;
    private Text label;
    private Transform player;

    public static TreadShredFireHint Show(Font font, Transform player)
    {
        // Taps pass through it: the player fires by tapping the battlefield, and taps on UI do not fire. It draws
        // under the GET READY card, faint during the countdown and clear once play starts.
        var root = TreadShredUi.CreateOverlay("TreadShredFireHint", -2, receivesTaps: false);
        var hint = root.gameObject.AddComponent<TreadShredFireHint>();
        hint.root = root;
        hint.player = player;
        hint.label = TreadShredUi.CreateText(FirstMinute.FireHint, root, Vector2.zero, new Vector2(520f, 56f), 34, font);
        hint.label.color = TreadShredUi.TitleColor;
        hint.label.gameObject.SetActive(false);
        return hint;
    }

    private void LateUpdate()
    {
        if (ManagerScore.instance == null || ManagerScore.instance.CurCountbullet > 0)
        {
            Destroy(gameObject);
            return;
        }
        var target = NearestEnemy();
        var view = Camera.main;
        var screen = target != null && view != null ? view.WorldToScreenPoint(target.position) : Vector3.back;
        if (screen.z <= 0f)
        {
            label.gameObject.SetActive(false);
            return;
        }
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var local);
        label.rectTransform.anchoredPosition = local + new Vector2(0f, 90f);
        label.gameObject.SetActive(true);
    }

    private Transform NearestEnemy()
    {
        Transform nearest = null;
        var best = float.MaxValue;
        foreach (var tag in EnemyTags)
        {
            foreach (var enemy in GameObject.FindGameObjectsWithTag(tag))
            {
                // Each enemy's model child carries its tag too, without a collider; destroyed enemies lose theirs.
                var body = enemy.GetComponent<BoxCollider>();
                if (body == null || !body.enabled)
                    continue;
                var distance = player == null ? 0f : (enemy.transform.position - player.position).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = enemy.transform;
                }
            }
        }
        return nearest;
    }
}
