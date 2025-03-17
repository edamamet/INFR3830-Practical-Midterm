using UnityEngine;

namespace _Project.Scripts {
    public class Cube : MonoBehaviour {
        [SerializeField] float speed = 2f;
        void Update() {
            if (GameManager.Mode != Mode.Move) return;
            Vector2 input = new(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            input = input.normalized;
            transform.Translate(
                input.x * Time.deltaTime * speed,
                0,
                input.y * Time.deltaTime * speed
            );
            
            GameManager.Position = transform.position;
        }
    }
}
