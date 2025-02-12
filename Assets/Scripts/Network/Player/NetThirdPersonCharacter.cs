using UnityEngine;
using Unity.Netcode;

namespace UnityStandardAssets.Characters.ThirdPerson
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class NetThirdPersonCharacter : NetworkBehaviour
    {
        [SerializeField] private float m_MovingTurnSpeed = 360;
        [SerializeField] private float m_StationaryTurnSpeed = 180;
        [SerializeField] private float m_JumpPower = 6f;
        [Range(1f, 4f)][SerializeField] private float m_GravityMultiplier = 2f;
        [SerializeField] private float m_GroundCheckDistance = 0.3f;
        [SerializeField] private float m_MoveSpeed = 3f;

        private Rigidbody m_Rigidbody;
        private bool m_IsGrounded;
        private float m_OrigGroundCheckDistance;

        void Start()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            if (m_Rigidbody == null)
            {
                Debug.LogError("Rigidbody is missing on NetThirdPersonCharacter.");
                return;
            }

            m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            m_OrigGroundCheckDistance = m_GroundCheckDistance;
        }

        public void Move(float moveX, float moveZ, bool jump)
        {
            if (!IsOwner || m_Rigidbody == null) return;

            Vector3 move = new Vector3(moveX, 0, moveZ);
            if (move.magnitude > 1f) move.Normalize();

            move = transform.TransformDirection(move) * m_MoveSpeed;
            CheckGroundStatus();

            ApplyMove(moveX, moveZ, jump);
            MoveNetworkedServerRpc(moveX, moveZ, jump);
        }

        private void ApplyMove(float moveX, float moveZ, bool jump)
        {
            if (m_Rigidbody == null) return;

            if (m_IsGrounded)
            {
                m_Rigidbody.velocity = new Vector3(moveX * m_MoveSpeed, m_Rigidbody.velocity.y, moveZ * m_MoveSpeed);
                if (jump)
                    m_Rigidbody.velocity = new Vector3(m_Rigidbody.velocity.x, m_JumpPower, m_Rigidbody.velocity.z);
            }
            else
            {
                HandleAirborneMovement();
            }

            ApplyExtraTurnRotation(moveX, moveZ);
        }

        [ServerRpc(RequireOwnership = false)]
        private void MoveNetworkedServerRpc(float moveX, float moveZ, bool jump)
        {
            MoveNetworkedClientRpc(moveX, moveZ, jump);
        }

        [ClientRpc]
        private void MoveNetworkedClientRpc(float moveX, float moveZ, bool jump)
        {
            if (IsOwner || m_Rigidbody == null) return;

            Vector3 move = new Vector3(moveX, 0, moveZ);
            if (move.magnitude > 1f) move.Normalize();

            move = transform.TransformDirection(move) * m_MoveSpeed;
            CheckGroundStatus();

            ApplyMove(moveX, moveZ, jump);
        }

        private void HandleAirborneMovement()
        {
            if (m_Rigidbody == null) return;

            Vector3 extraGravityForce = (Physics.gravity * m_GravityMultiplier) - Physics.gravity;
            m_Rigidbody.AddForce(extraGravityForce);
            m_GroundCheckDistance = m_Rigidbody.velocity.y < 0 ? m_OrigGroundCheckDistance : 0.01f;
        }

        private void ApplyExtraTurnRotation(float moveX, float moveZ)
        {
            if (moveX == 0 && moveZ == 0) return;

            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(moveX, 0, moveZ));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * m_MovingTurnSpeed);
        }

        private void CheckGroundStatus()
        {
            if (m_Rigidbody == null) return;

            RaycastHit hitInfo;
#if UNITY_EDITOR
            Debug.DrawLine(transform.position + (Vector3.up * 0.1f), transform.position + (Vector3.up * 0.1f) + (Vector3.down * m_GroundCheckDistance), Color.red);
#endif
            if (Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, out hitInfo, m_GroundCheckDistance))
            {
                m_IsGrounded = true;
            }
            else
            {
                m_IsGrounded = false;
            }
        }
    }
}
