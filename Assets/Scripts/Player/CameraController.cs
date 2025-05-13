using UnityEngine;

namespace Player
{
    public class CameraController : MonoBehaviour
    {
        public Transform player;            // Referencia al jugador
        public Vector3 offset = new Vector3(0f, 2f, -5f);  // Desplazamiento desde el jugador
        public float rotationSpeed = 2.5f;    // Velocidad de rotaci�n
        public float pitchSpeed = 1f;       // Velocidad de inclinaci�n (eje Y)
        public float minPitch = -20f;       // �ngulo m�nimo de inclinaci�n
        public float maxPitch = 50f;        // �ngulo m�ximo de inclinaci�n

        private float yaw = 0f;             // Rotaci�n alrededor del eje Y
        private float pitch = 2f;           // Inclinaci�n hacia arriba/abajo (eje X)

        private Vector2 _cameraMove = Vector2.zero;

        private void OnEnable()
        {
            GameInput.InputReader.Instance.onCameraMove += CameraMove;
        }
        private void OnDisable()
        {
            GameInput.InputReader.Instance.onCameraMove -= CameraMove;
        }

        void LateUpdate()
        {
            if (player == null)
            {
                Debug.LogWarning("Player reference is missing.");
                return;
            }

            HandleCameraRotation();
            UpdateCameraPosition();
        }

        private void CameraMove(Vector2 cameraPos)
        {
            _cameraMove = cameraPos;
        }

        private void HandleCameraRotation()
        {
            //_cameraMove /= 10; // To reduce the scale a little
            float mouseX = _cameraMove.x * rotationSpeed;
            float mouseY = _cameraMove.y * pitchSpeed;


            // Modificar los �ngulos de rotaci�n (yaw y pitch)
            yaw += mouseX;
            pitch -= mouseY;

            // Limitar la inclinaci�n de la c�mara
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        private void UpdateCameraPosition()
        {
            // Calcular la nueva direcci�n de la c�mara
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 rotatedOffset = rotation * offset;

            // Posicionar la c�mara en funci�n del jugador y el nuevo offset
            transform.position = player.position + rotatedOffset;

            // Siempre mirar al jugador
            transform.LookAt(player);
        }
    }

}
