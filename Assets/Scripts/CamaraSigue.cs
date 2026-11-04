using UnityEngine;

// Se pone en la Main Camera. Hace que la cámara siga al águila.
public class CamaraSigue : MonoBehaviour
{
    [SerializeField] private Transform objetivo;   // el águila (se asigna en el Inspector)

    // LateUpdate: corre después de que el águila ya se movió, evita temblores
    void LateUpdate()
    {
        if (objetivo == null) return;   // si no hay objetivo, no hace nada

        // Copia x e y del águila, pero conserva la z de la cámara (-10)
        transform.position = new Vector3(objetivo.position.x, objetivo.position.y, transform.position.z);
    }
}
