using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed;
    [SerializeField] private float rotationSpeed;
    [HideInInspector] public Vector3 moveDir;
    private Rigidbody rb;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    [Header("Jump")]
    [SerializeField] private float jumpPower;
    private float jumpDelay;
    private float jumpInputDelay;
    [SerializeField] private float upGravity;
    [SerializeField] private float downGravity;
    [SerializeField] private float hangGravity;
    [SerializeField] private float hangPoint;
    public bool grounded;

    [Header("Health")]
    [SerializeField] private int health;
    [SerializeField] private int maxHealth;
    public Transform hpBar;
    [SerializeField] private float maxBurstDmg;
    private float immunityTimer;
    private int currentBurst;
    public bool canDie;

    [Header("Shield")]
    [HideInInspector] public float shieldTimer;
    [SerializeField] private GameObject shield;

    [Header("Misc")]
    [SerializeField] private Animator anim;
    [SerializeField] private Animator damageFlash;
    //Game Over
    [SerializeField] private GameObject gameOver;
    private bool endingGame;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        health = maxHealth;
    }


    void Update()
    {
        //Anim Values
        Bounds b = groundCheck.GetComponent<BoxCollider>().bounds;
        //grounded = ((Physics.OverlapBox(b.center, b.extents*2, Quaternion.identity, groundLayer) != null) && jumpDelay == 0);
        grounded = (Physics.CheckSphere(groundCheck.position, 0.1f, groundLayer) && jumpDelay == 0);
        //anim.SetBool("airborne", !grounded);
        //anim.SetBool("jumpDelay", jumpDelay > 0);
        //anim.SetFloat("yVel", rb.velocity.y);

        //Jump
        jumpDelay = Mathf.Max(0, jumpDelay-Time.deltaTime);
        jumpInputDelay = Mathf.Max(0, jumpInputDelay-Time.deltaTime);
        if (GetComponent<PlayerInteract>() != null)
        {
            if (Input.GetKey(KeyCode.Space) && !GetComponent<PlayerInteract>().planting)
                jumpInputDelay = 0.3f;
        }
        else if (Input.GetKey(KeyCode.Space))
            jumpInputDelay = 0.3f;
        if (grounded && jumpInputDelay > 0 && jumpDelay == 0 && !GameManager.Instance.pauseGame && !GameManager.Instance.playerPaused)
        {
            jumpDelay = 0.3f;
            StartCoroutine(JumpAnim());
        }

        //Glitch based on HP
        if (!Camera.main.GetComponent<GlitchManager>().showingGlitch)
            Camera.main.GetComponent<Glitch>().glitch = Mathf.Lerp(0, 0.3f, Mathf.Pow((maxHealth-health)/(1f*maxHealth), 3));


        immunityTimer = Mathf.Max(0, immunityTimer-Time.deltaTime);
        shieldTimer = Mathf.Max(0, shieldTimer-Time.deltaTime);
        shield.SetActive(shieldTimer > 0);
    }


    void FixedUpdate()
    {
        //Apply gravity
        if (Mathf.Abs(rb.velocity.y) < hangPoint)
            rb.AddForce(-9.8f * hangGravity * Vector3.up, ForceMode.Acceleration);
        else if (rb.velocity.y < 0)
            rb.AddForce(-9.8f * downGravity * Vector3.up, ForceMode.Acceleration);
        else
            rb.AddForce(-9.8f * upGravity * Vector3.up, ForceMode.Acceleration);

        //Movement
        int lateral = 0;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            lateral ++;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            lateral --;
        int forward = 0;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            forward ++;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            forward --;

        Vector3 camForward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized;
        moveDir = (lateral*camRight + forward*camForward).normalized;
        if (moveDir != Vector3.zero && !GameManager.Instance.pauseGame && !GameManager.Instance.playerPaused && !GetComponent<PlayerPrograms>().dashing)
        {
            float spd = speed;
            float rotSpd = rotationSpeed;
            rb.MovePosition(rb.position + moveDir * spd * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), rotSpd * Time.deltaTime);
        }
    }


    private IEnumerator JumpAnim()
    {
        anim.Play("Jump");
        yield return new WaitForSeconds(0.1f);
        rb.velocity = new Vector3(rb.velocity.x, jumpPower, rb.velocity.z);
    }

    public void TakeDamage(int dmg)
    {
        if (immunityTimer == 0 && shieldTimer == 0)
        {
            //keep track of damage taken in last 0.5s & give immunity if past burst threshold
            currentBurst += dmg;
            StartCoroutine(UndoBurst(dmg));
            if (currentBurst > maxBurstDmg)
            {
                immunityTimer = 0.5f;
            }

            //cancel terminal progress
            StopCoroutine(GameManager.Instance.UseTerminal());
            AudioManager.Instance.Stop("Terminal Charge");
            if (GameManager.Instance.bar != null)
                Destroy(GameManager.Instance.bar.transform.parent.gameObject);
            GameManager.Instance.playerPaused = false;

            //take damage
            health = Mathf.Max(0, health-dmg);
            if (health <= 0)
            {
                Debug.Log("GAME OVER!!");
                if (canDie)
                {
                    if (!endingGame)
                        StartCoroutine(GameOver());
                }
                else
                {
                    health = maxHealth;
                }
            }
            else
            {
                AudioManager.Instance.Play("Take Damage");
                damageFlash.Play("DamageFlash");
                Camera.main.GetComponent<GlitchManager>().ShowGlitch(0.5f, 0.5f);
            }
            
            if (hpBar != null)
            {
                hpBar.GetChild(1).GetComponent<Image>().fillAmount = health/(maxHealth*1.0f);
                hpBar.GetChild(2).GetComponent<TMPro.TextMeshProUGUI>().text = health + "/" + maxHealth;
            }
        }
    }

    private IEnumerator UndoBurst(int dmg)
    {
        yield return new WaitForSeconds(0.5f);
        currentBurst -= dmg;
    }

    public IEnumerator GameOver()
    {
        endingGame = true;
        GetComponent<PlayerPrograms>().enabled = false;
        GameManager.Instance.pauseGame = true;
        StartCoroutine(AudioManager.Instance.FadeOutAll(0));
        AudioManager.Instance.Play("Static");
        AudioManager.Instance.Play("Game Over");
        Camera.main.GetComponent<GlitchManager>().ShowGlitch(2, 1);
        yield return new WaitForSeconds(2);
        AudioManager.Instance.Stop("Static");
        gameOver.SetActive(true);
        yield return new WaitForSeconds(1);
        TMPro.TextMeshProUGUI txt = gameOver.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
        for (int i = 0; i < 4; i++)
        {
            txt.text = "_";
            yield return new WaitForSeconds(0.5f);
            txt.text = "";
            yield return new WaitForSeconds(0.3f);
        }
        yield return new WaitForSeconds(1);
        string message = "Program Terminated";
        foreach (char c in message)
        {
            txt.text += c;
            if (c == ' ')
                yield return new WaitForSeconds(0.1f);
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(1.5f);
        Fader.Instance.FadeIn(1.5f);
        StartCoroutine(AudioManager.Instance.StartFade("Game Over", 2, 0));
        yield return new WaitForSeconds(2f);
        endingGame = false;
        SceneManager.LoadScene("Playtest Options");
    }
}