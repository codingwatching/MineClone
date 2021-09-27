using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private Camera mainCam;

    [SerializeField] private float verticalSens = 1f;

    [SerializeField] private float horizontalSens = 1f;

    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float playerTerminalVelocity = -25f;
    [SerializeField] private float gravityConst = 9.8f;
    
    [SerializeField] private float jumpHeight = 1.125f;
    [SerializeField] private float waterSlowModifier = 0.7f;

    [SerializeField] private InventoryView _inventoryView;

    private CharacterController _characterController;
    private Vector3 _playerVelocity = Vector3.zero;
    private Vector3 _downVelocity = new Vector3(0,0,0);

    private float smoothedValX = 0f;
    private float smoothedValZ = 0f;

    private TerrainGen _terrainGen;

    private float rotationY = 0;

    private bool isInWater = false;
    private bool lastFrameInWater = false;

    public bool IsInWater
    {
        get => isInWater;
        private set => isInWater = value;
    }
    
    void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _terrainGen = FindObjectOfType<TerrainGen>();
    }

    // Update is called once per frame
    void Update()
    {
        ProcessCamera();
        ProcessMovement();
    }

    private void FixedUpdate()
    {
        lastFrameInWater = isInWater;
        isInWater = _terrainGen.BlockTypeFromPosition(transform.position) == BlockTypes.WATER_SOURCE;
    }

    private void ProcessMovement()
    {
        if (!_characterController.isGrounded)
        {
            var change = -gravityConst * Time.deltaTime;
            if (isInWater)
                change *= waterSlowModifier * waterSlowModifier;
            _downVelocity.y += change;
        }

        //Only process player inputs when inventory is closed
        if (!_inventoryView.IsInventoryActive)
        {
            _playerVelocity =
                new Vector3(Input.GetAxis("Horizontal") * moveSpeed, 0, Input.GetAxis("Vertical") * moveSpeed);
            Vector3.ClampMagnitude(_playerVelocity, moveSpeed);

            //Transform local direction vector into world vector
            _playerVelocity = transform.TransformDirection(_playerVelocity);

            if (isInWater)
            {
                _playerVelocity *= waterSlowModifier;
            }else if (!_characterController.isGrounded)
            {
                //TODO Use parabola equation to calculate movement in air
            }

            if (isInWater || lastFrameInWater)
            {
                if (Input.GetButton("Jump"))
                {
                    _downVelocity.y = Mathf.Sqrt(Mathf.Abs(2 * jumpHeight * gravityConst)) * waterSlowModifier;
                }
            }
            else
            {
                if (Input.GetButtonDown("Jump") && _characterController.isGrounded)
                {
                    _downVelocity.y = Mathf.Sqrt(Mathf.Abs(2 * jumpHeight * gravityConst));
                }
            }
        }
        else
        {
            if (!Mathf.Approximately(Mathf.Abs(_playerVelocity.x),0) || !Mathf.Approximately(Mathf.Abs(_playerVelocity.z), 0))
            {
                //Smoothen the player stop when player opens inventory, not abrupt stop
                if(!Mathf.Approximately(Mathf.Abs(_playerVelocity.x),0))
                {
                    _playerVelocity.x = Mathf.SmoothDamp(_playerVelocity.x, 0f, ref smoothedValX, Mathf.Abs(_playerVelocity.x)/moveSpeed/3f);
                }
                else
                {
                    smoothedValX = 0f;
                }
                if(!Mathf.Approximately(Mathf.Abs(_playerVelocity.z), 0))
                {
                    _playerVelocity.z = Mathf.SmoothDamp(_playerVelocity.z, 0f, ref smoothedValZ, Mathf.Abs(_playerVelocity.z)/moveSpeed/3f);
                }
                else
                {
                    smoothedValZ = 0f;
                }
            }
        }
        
        //Clamp to terminal velocity
        _downVelocity.y = Mathf.Clamp(_downVelocity.y, playerTerminalVelocity, Mathf.Infinity);

        //Move takes in distance moved. Velocity times time equals distance
        _characterController.Move((_downVelocity + _playerVelocity) * Time.deltaTime);
    }

    private void ProcessCamera()
    {
        //Only process player inputs when inventory is closed
        if (_inventoryView.IsInventoryActive)
            return;
        
        //Vertical rotation
        rotationY -= Input.GetAxis("Mouse Y") * verticalSens * Time.deltaTime;
        rotationY = Mathf.Clamp(rotationY, -90, 90);
        mainCam.transform.localRotation = Quaternion.Euler(new Vector3(rotationY, 0, 0));

        //Horizontal rotation
        transform.Rotate(Vector3.up, Input.GetAxis("Mouse X") * horizontalSens * Time.deltaTime);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("WaterChunk"))
        {
            IsInWater = true;
        } 
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("WaterChunk"))
        {
            IsInWater = false;
        } 
    }
}
