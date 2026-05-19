using JetBrains.Annotations;
using UnityEngine;




public abstract class NPCAnimator : MonoBehaviour {
    public abstract void FaceDirection(Direction d);


    public abstract void SetVisual(bool v);

    public abstract void StopWalking();

    public abstract void StartWalking();

    public abstract void SetTrainerVisual(NPCTrainerVisual npcVisual);

    public abstract void SetSprite(Sprite s);

    //  public virtual void ShowReflection() {

    // }

    // public virtual void HideReflection() {
    //     
    // }

    public virtual void SetWalkSpeed(float walkSpeedMultiple) {

    }

    public virtual void SetReflection(bool v) {

    }


}




public enum Direction { UP = 0, RIGHT = 1, DOWN = 2, LEFT = 3 }
public class RPGMakerAnimator : NPCAnimator {

    public static Direction VecToDirection(Vector3Int c) {
        if (c == Vector3Int.up) {
            return Direction.UP;
        } else if (c == Vector3Int.down) {
            return Direction.DOWN;
        } else if (c == Vector3Int.left) {
            return Direction.LEFT;
        } else if (c == Vector3Int.right) {
            return Direction.RIGHT;
        }
        return Direction.UP;
    }
    public static Vector3Int DirectionToVec(Direction d) {
        switch (d) {
            case Direction.UP:
                return Vector3Int.up;

            case Direction.RIGHT:
                return Vector3Int.right;

            case Direction.DOWN:
                return Vector3Int.down;

            case Direction.LEFT:
                return Vector3Int.left;

        }
        Debug.LogError("SHOULDN'T GET HERE");
        return Vector3Int.zero;
    }




    [SerializeField] private Animator anim;
    [SerializeField] private Animator refelctionAnim;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteRenderer reflectionSpriteRenderer;
    
    private Direction direction;
    private bool b;
    private void Awake() {
        anim.keepAnimatorStateOnDisable = true;
        if (reflectionSpriteRenderer) refelctionAnim.keepAnimatorStateOnDisable = true;
        
    }

    public const string idle = "Idle", horizontal = "HorizontalDirection", vertical = "VerticallDirection";
    public override void FaceDirection(Direction d) {

        

        direction = d;
        anim.SetFloat(horizontal, DirectionToVec(d).x);
        anim.SetFloat(vertical, DirectionToVec(d).y);
        if (reflectionSpriteRenderer && refelctionAnim.runtimeAnimatorController != null && refelctionAnim.gameObject.activeInHierarchy) refelctionAnim.SetFloat(horizontal, DirectionToVec(d).x);
        if (reflectionSpriteRenderer && refelctionAnim.runtimeAnimatorController != null && refelctionAnim.gameObject.activeInHierarchy) refelctionAnim.SetFloat(vertical, DirectionToVec(d).y);

    }

    public override void SetSprite(Sprite s) {
        anim.GetComponent<SpriteRenderer>().sprite = s;
    }

    public override void SetVisual(bool v) {
        spriteRenderer.enabled = v;
        if (reflectionSpriteRenderer)reflectionSpriteRenderer.enabled = v;
        anim.enabled = v;
        if (reflectionSpriteRenderer && refelctionAnim.runtimeAnimatorController != null) refelctionAnim.enabled = v;
    }

    public override void StopWalking() {
        //print("Stop Walking");
        anim.SetBool(idle, true);
        //anim.Play("Idle");
        if (reflectionSpriteRenderer && refelctionAnim.runtimeAnimatorController != null && refelctionAnim.gameObject.activeInHierarchy) refelctionAnim.SetBool(idle, true);
    }

    public override void StartWalking() {
        //  print("Start Walking");
       // Debug.Log("SWALKING " + Time.time.ToString());
        anim.SetBool(idle, false);
        if (reflectionSpriteRenderer && refelctionAnim.gameObject.activeInHierarchy) refelctionAnim.SetBool(idle, false);
    }

    public override void SetTrainerVisual(NPCTrainerVisual npcVisual) {
        if (npcVisual == null) Debug.Break();
        anim.runtimeAnimatorController = npcVisual.animator;
        if (refelctionAnim != null) refelctionAnim.runtimeAnimatorController = npcVisual.animator;
    }

  

    // public override void HideReflection() {
    //      if (refelctionAnim.gameObject != null) refelctionAnim.gameObject.SetActive(false);
    // }

    // public override void ShowReflection() {
    //    if (refelctionAnim.gameObject != null) refelctionAnim.gameObject.SetActive(true);
    // }

    public override void SetWalkSpeed(float walkSpeedMultiple) {
        anim.speed = walkSpeedMultiple;
    }

    public override void SetReflection(bool v) {
        if (reflectionSpriteRenderer != null) {
            refelctionAnim.gameObject.SetActive(v);
        }
    }
}
