using UnityEngine;
using System.Collections.Generic;

public class TestNPCSceneController : MonoBehaviour
{
    public List<NPCTrainerVisual> visuals = new List<NPCTrainerVisual>();

    public Transform attach;

    public float moveSpeed = 5f;

    public int between = 1;
    public int colCount = 4;

    public float maxDistance = 10f;

    public Animator animPrefab;

    public List<Animator> spawnedAnims = new List<Animator>();


    private void Awake() {


        int y = 0;

        while (visuals.Count > 0) {

            for (int x = 0; x < colCount; x++) {
                if (visuals.Count == 0) {
                    break;
                }

                NPCTrainerVisual visual = visuals[0];
                visuals.Remove(visual);

                int rows = visuals.Count / colCount;

                Vector3 startPoint = new Vector3((colCount / -2f) * between, (rows / -2f) * between, 0);

                Animator anim = Instantiate(animPrefab, startPoint + new Vector3(x,y, 0) * between, Quaternion.identity,  attach);
                anim.runtimeAnimatorController = visual.animator;
                anim.gameObject.SetActive(true);
                spawnedAnims.Add(anim);

            }
            y++;


        }



    }



    private void Update() {




        Vector2 v = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));


        if (v != Vector2.zero) {

            foreach (Animator anim in spawnedAnims) {
                anim.SetBool(RPGMakerAnimator.idle, false);
                anim.SetFloat(RPGMakerAnimator.horizontal, v.x);
                anim.SetFloat(RPGMakerAnimator.vertical, v.y);
            }


        } else {
            foreach (Animator anim in spawnedAnims) {
                anim.SetBool(RPGMakerAnimator.idle, true);
            }
        }







    }




}
