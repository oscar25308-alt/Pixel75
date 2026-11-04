using UnityEngine;

// Clase en PascalCase. El archivo se llama igual: AguilaController.cs
public class AguilaController : MonoBehaviour
{
    // ---------- VARIABLES ----------
    [SerializeField] private float velocidad = 5f;   // float: rapidez del águila, editable en el Inspector
    [SerializeField] private int puntosGema = 10;    // int: puntos que suma una gema
    [SerializeField] private int puntosCereza = -5;  // int: puntos que resta una cereza (valor negativo)

    private int puntaje = 0;            // int: marcador, solo uso interno
    private Rigidbody2D rb;             // referencia al componente de física del águila
    private SpriteRenderer dibujo;      // referencia al sprite, para voltearlo según la dirección
    private Vector2 direccion;          // dirección (x, y) del movimiento

    // ---------- MÉTODOS DE UNITY ----------
    // Start: se ejecuta una sola vez al iniciar
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();        // se obtiene una sola vez
        dibujo = GetComponent<SpriteRenderer>();
    }

    // Update: se ejecuta cada frame; aquí solo LEEMOS las teclas
    void Update()
    {
        float x = 0f;   // variables locales: -1, 0 o 1
        float y = 0f;

        if (Input.GetKey(KeyCode.W)) y = 1f;    // arriba
        if (Input.GetKey(KeyCode.S)) y = -1f;   // abajo
        if (Input.GetKey(KeyCode.A)) x = -1f;   // izquierda
        if (Input.GetKey(KeyCode.D)) x = 1f;    // derecha

        // normalized: en diagonal el vector mide 1, no 1.41 -> no va más rápido
        direccion = new Vector2(x, y).normalized;

        // El sprite original mira a la izquierda: se voltea al ir a la derecha
        if (x != 0f) dibujo.flipX = x > 0f;
    }

    // FixedUpdate: intervalos fijos de física; aquí APLICAMOS la velocidad
    void FixedUpdate()
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = direccion * velocidad;   // Unity 6
#else
        rb.velocity = direccion * velocidad;         // versiones anteriores
#endif
    }

    // Se llama cuando el águila toca un collider marcado como Trigger
    void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag("Gema"))
        {
            CambiarPuntaje(puntosGema);
            Destroy(otro.gameObject);   // la gema desaparece
        }
        else if (otro.CompareTag("Cereza"))
        {
            CambiarPuntaje(puntosCereza);
            Destroy(otro.gameObject);   // la cereza desaparece
        }
    }

    // ---------- MÉTODO PROPIO ----------
    // Recibe un int (parámetro) y no devuelve nada (void)
    void CambiarPuntaje(int cantidad)
    {
        puntaje += cantidad;
        Debug.Log("Puntaje: " + puntaje);   // muestra el puntaje en la consola
    }
}
