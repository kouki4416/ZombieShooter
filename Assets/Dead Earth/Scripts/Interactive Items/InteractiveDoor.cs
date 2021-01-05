using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//---Enum---
public enum InteractiveDoorAxisAlignment { XAxis, YAxis, ZAxis }


//-----------------------------------------------------------------------------------------
// CLASS    :   InteractiveDoorInfo
// DESC     :   Describes the animation properties of a single door in an InteractiveDoor
//-----------------------------------------------------------------------------------------
[System.Serializable]
public class InteractiveDoorInfo
{
    // Transform to animate
    public Transform Transform = null;
    // Local rotation axis and amount
    public Vector3 Rotation = Vector3.zero;
    // Local axis of movement and distance along that axis
    public Vector3 Movement = Vector3.zero;

    // Following are used to cache the open and closed position and rotations of the door
    [HideInInspector]
    public Quaternion ClosedRotation = Quaternion.identity;
    [HideInInspector]
    public Quaternion OpenRotation = Quaternion.identity;
    [HideInInspector]
    public Vector3 OpenPosisiton = Vector3.zero;
    [HideInInspector]
    public Vector3 ClosedPosisiton = Vector3.zero;
}


//-----------------------------------------------------------------------------------------
// CLASS    :   InteractiveRotatingDoor
// DESC     :   Controll mechanism for all rotating door constructs
//-----------------------------------------------------------------------------------------
[RequireComponent(typeof(BoxCollider))]
public class InteractiveDoor : InteractiveItem
{
//----------Inspector assigned variables----------
    [Header("Activation Properties")]
    [Tooltip("Does the door start open or closed")]
    [SerializeField] protected bool _isClosed = true;

    [Tooltip("Does the door open in both directions")]
    [SerializeField] protected bool _isTwoWay = true;

    [Tooltip("Does the door open automatically when the player walks into its trigger")]
    [SerializeField] protected bool _autoOpen = false;

    [Tooltip("Does the door close automatically after a period of time")]
    [SerializeField] protected bool _autoClose = false;

    [Tooltip("Random time range  for auto close")]
    [SerializeField] protected Vector2 _autoCloseDelay = new Vector2(5.0f, 5.0f);

    [Tooltip("The size of the box collider after the door is open")]
    [SerializeField] protected float _colliderLengthOpenScale = 3.0f;

    [Tooltip("Should we offset the center of the collider when open")]
    [SerializeField] protected bool _offsetCollider = true;

    [Tooltip("A container object used as a parent for any objects the open door should reveal")]
    [SerializeField] protected Transform _contentMount = null;

    //The local forward axis of this object
    [SerializeField] protected InteractiveDoorAxisAlignment _localForwardAxis = InteractiveDoorAxisAlignment.ZAxis;


    //Key values that must be set in app database for door to open
    [Header("Game State Management")]
    [SerializeField] protected List<GameState> _requiredStates = new List<GameState>();
    [SerializeField] protected List<string> _requiredItems = new List<string>();


    //Text messages to return to HUD based of the status of door
    [Header("Message")]
    [TextArea(3, 10)]
    [SerializeField] protected string _openedHintText = "Door: Press 'Use' to close";
    [TextArea(3, 10)]
    [SerializeField] protected string _closedHintText = "Door: Press 'Use' to open";
    [TextArea(3, 10)]
    [SerializeField] protected string _cantActivateHintText = "Door: It's locked";


    //How the door should behave for each action
    [Header("Door Transforms")]
    [Tooltip("A list of child transforms to animate")]
    [SerializeField] protected List<InteractiveDoorInfo> _doors = new List<InteractiveDoorInfo>();


//----------Private----------
    //To keep open and close coroutine
    protected IEnumerator _coroutine = null;
    protected Vector3 _closedColliderSize = Vector3.zero;
    protected Vector3 _closedColliderCenter = Vector3.zero;
    protected Vector3 _openColliderSize = Vector3.zero;
    protected Vector3 _openColliderCenter = Vector3.zero;
    protected BoxCollider _boxCollider = null;
    protected Plane _plane;
    //Used for storing info about the doorr progrress during the coroutine(t = 0 to 1)
    protected float _normalizedTime = 0.0f;


//----------Function----------
    //-----------------------------------------------------------------------------------------
    // NAME    :   GetText
    // DESC    :   Return a string of text to display on the HUD when the playe is inspecting this door
    //-----------------------------------------------------------------------------------------
    public override string GetText()
    {
        // Need to test all states required are satisfied
        bool haveInventoryItems = HaveRequiredInvItems();
        bool haveRequiredStates = true;

        //Check the states are set in application state database
        if(_requiredStates.Count > 0)
        {
            if (ApplicationManager.instance == null) haveRequiredStates = false;
            else
                haveRequiredStates = ApplicationManager.instance.AreStatesSet(_requiredStates);
        }

        // What text to return
        if(_isClosed)
        {
            if(!haveRequiredStates || !haveInventoryItems)
            {
                return _cantActivateHintText;
            }
            else
            {
                return _closedHintText;
            }
        }
        else
        {
            return _openedHintText;
        }
    }


    // TODO after inventory
    protected bool HaveRequiredInvItems()
    {
        return true;
    }

    //-----------------------------------------------------------------------------------------
    // NAME    :   Start
    // DESC    :   Called at start and set up initial collider pos, rot...etc
    //-----------------------------------------------------------------------------------------
    protected override void Start()
    {
        // register as interactive item in parent
        base.Start();

        // Cache components
        _boxCollider = _collider as BoxCollider;

        // Calculate the open and closed collider size and center
        if(_boxCollider != null)
        {
            _closedColliderSize = _openColliderSize = _boxCollider.size;
            _closedColliderCenter = _openColliderCenter = _boxCollider.center;
            float offset = 0.0f;

            // Make sure we offset the collider and grow into the dimention specified by forward axis
            switch(_localForwardAxis)
            {
                case InteractiveDoorAxisAlignment.XAxis:
                    _plane = new Plane(transform.right, transform.position);
                    _openColliderSize.x *= _colliderLengthOpenScale;
                    offset = _closedColliderCenter.x - (_openColliderSize.x / 2.0f);
                    _openColliderCenter = new Vector3(offset, _closedColliderCenter.y, _closedColliderCenter.z);
                    break;
                case InteractiveDoorAxisAlignment.YAxis:
                    _plane = new Plane(transform.up, transform.position);
                    _openColliderSize.y *= _colliderLengthOpenScale;
                    offset = _closedColliderCenter.y - (_openColliderSize.y / 2.0f);
                    _openColliderCenter = new Vector3(_closedColliderCenter.x, offset, _closedColliderCenter.z);
                    break;
                case InteractiveDoorAxisAlignment.ZAxis:
                    _plane = new Plane(transform.forward, transform.position);
                    _openColliderSize.z *= _colliderLengthOpenScale;
                    offset = _closedColliderCenter.z - (_openColliderSize.z / 2.0f);
                    _openColliderCenter = new Vector3(_closedColliderCenter.x, _closedColliderCenter.y, offset);
                    break;
            }

            //If we are strting Open then apply open scale and offsets to collider
            if(!_isClosed)
            {
                _boxCollider.size = _openColliderSize;
                if (_offsetCollider) _boxCollider.center = _openColliderCenter;
            }
        }

        // Set all of the doors to starting orientations
        foreach(InteractiveDoorInfo door in _doors)
        {
            if(door != null && door.Transform != null)
            {
                // Assume all door is closed at start, so grab current rotation as closed one
                door.ClosedRotation = door.Transform.localRotation;
                door.ClosedPosisiton = door.Transform.position;
                door.OpenPosisiton = door.Transform.position - door.Transform.TransformDirection(door.Movement);

                // Calculate a rotation to take into open position
                Quaternion rotationToOpen = Quaternion.Euler(door.Rotation);

                // If the door is to start open then rotate/move to open
                if(!_isClosed)
                {
                    door.Transform.localRotation = door.ClosedRotation * rotationToOpen;
                    door.Transform.position = door.OpenPosisiton;
                }
            }
        }

        // Disable colliders of any contents if in the closed position
        if(_contentMount != null)
        {
            Collider[] colliders = _contentMount.GetComponentsInChildren<Collider>();
            foreach(Collider col in colliders)
            {
                if (_isClosed)
                    col.enabled = false;
                else
                    col.enabled = true;
            }
        }

        // Animation is not currently in progress
        _coroutine = null;
    }

    //-----------------------------------------------------------------------------------------
    // NAME    :   Activate
    // DESC    :   Called by character manager to active this door.
    //-----------------------------------------------------------------------------------------
    public override void Activate(CharacterManager characterManager)
    {
        // Check the required states.
        bool haveRequiredStates = true;
        if(_requiredStates.Count > 0)
        {
            if (ApplicationManager.instance == null) haveRequiredStates = false;
            else
                haveRequiredStates = ApplicationManager.instance.AreStatesSet(_requiredStates);
        }

        // Only activate the door if all requirements met
        if(haveRequiredStates && HaveRequiredInvItems())
        {
            // Stop any animation currently running and new anim
            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = Activate(_plane.GetSide(characterManager.transform.position));
            StartCoroutine(_coroutine);
        }
    }

    //-----------------------------------------------------------------------------------------
    // NAME    :   Activate(Coroutine)
    // DESC    :   This is the function that perform the actual door anim
    //-----------------------------------------------------------------------------------------
    private IEnumerator Activate(bool frontSide, bool autoClosing = false, float delay = 0.0f)
    {
        // Deal with delay
        yield return new WaitForSeconds(delay);

        // Sync for sound
        float duration = 1.5f;
        float time = 0.0f;

        // Set normalized time(which t you are currently at), for the situation of activating at 
        // half way of opening/closing door.
        if (_normalizedTime > 0.0f)
            _normalizedTime = 1 - _normalizedTime;

        // If the door is closed then open it
        if(_isClosed)
        {
            _isClosed = false;

            // Determine forward axis, offset, and scale of collider
            float offset = 0.0f;
            switch(_localForwardAxis)
            {
                case InteractiveDoorAxisAlignment.XAxis:
                    offset = _openColliderSize.x / 2.0f;
                    if (!frontSide) offset = -offset;
                    _openColliderCenter = new Vector3(_closedColliderCenter.x - offset, _closedColliderCenter.y, _closedColliderCenter.z);
                    break;
                case InteractiveDoorAxisAlignment.YAxis:
                    offset = _openColliderSize.y / 2.0f;
                    if (!frontSide) offset = -offset;
                    _openColliderCenter = new Vector3(_closedColliderCenter.x, _closedColliderCenter.y - offset, _closedColliderCenter.z);
                    break;
                case InteractiveDoorAxisAlignment.ZAxis:
                    offset = _openColliderSize.z / 2.0f;
                    if (!frontSide) offset = -offset;
                    _openColliderCenter = new Vector3(_closedColliderCenter.x, _closedColliderCenter.y, _closedColliderCenter.z - offset);
                    break;
            }

            if (_offsetCollider) _boxCollider.center = _openColliderCenter;
            _boxCollider.size = _openColliderSize;

            // Set starting time of anim
            time = duration * _normalizedTime;

            // Animate
            while(time <= duration)
            {
                foreach(InteractiveDoorInfo door in _doors)
                {
                    if(door != null && door.Transform != null)
                    {
                        _normalizedTime = time / duration;
                        // Calculate new pos and local rot
                        door.Transform.position = Vector3.Lerp(door.ClosedPosisiton, door.OpenPosisiton, _normalizedTime);
                        door.Transform.localRotation = door.ClosedRotation * Quaternion.Euler(frontSide || !_isTwoWay ? door.Rotation * _normalizedTime : -door.Rotation * _normalizedTime);
                    }
                }
                yield return null;
                time += Time.deltaTime;
            }

            // Enable colliders of any content
            if (_contentMount != null)
            {
                Collider[] colliders = _contentMount.GetComponentsInChildren<Collider>();
                foreach (Collider col in colliders)
                {
                    col.enabled = true;
                }
            }

            // Reset time since animation is done
            _normalizedTime = 0.0f;

            // If autoClose is active, spawn a new coroutine to close
            if(_autoClose)
            {
                _coroutine = Activate(frontSide, true, Random.Range(_autoCloseDelay.x, _autoCloseDelay.y));
            }
        }
        // Door is open so close it
        else
        {
            _isClosed = true;
            foreach (InteractiveDoorInfo door in _doors)
            {
                if (door != null && door.Transform != null)
                {
                    door.OpenRotation = door.Transform.localRotation;
                }
            }

            // Disable collider of contents
            if(_contentMount != null)
            {
                Collider[] colliders = _contentMount.GetComponentsInChildren<Collider>();
                foreach (Collider col in colliders)
                {
                    col.enabled = false;
                }
            }

            // Set starting time
            time = duration * _normalizedTime;

            // Close over time
            while(time <= duration)
            {
                foreach (InteractiveDoorInfo door in _doors)
                {
                    if (door != null && door.Transform != null)
                    {
                        _normalizedTime = time / duration;
                        // Calculate new pos and local rot
                        door.Transform.position = Vector3.Lerp(door.OpenPosisiton, door.ClosedPosisiton, _normalizedTime);
                        door.Transform.localRotation = Quaternion.Lerp(door.OpenRotation, door.ClosedRotation, _normalizedTime);
                    }
                }
                yield return null;
                time += Time.deltaTime;
            }

            foreach (InteractiveDoorInfo door in _doors)
            {
                if (door != null && door.Transform != null)
                {
                    // Set exactly to closed pos and rot to remove small value diffs over
                    // numbers of activations
                    door.Transform.localRotation = door.ClosedRotation;
                    door.Transform.position = door.ClosedPosisiton;
                }
            }

            _boxCollider.size = _closedColliderSize;
            _boxCollider.center = _closedColliderCenter;
        }

        _normalizedTime = 0.0f;
        _coroutine = null;
        yield break;
    }

    //-----------------------------------------------------------------------------------------
    // NAME    :   OnTriggerEnter
    // DESC    :   Automatically trigger the opening of a door
    //-----------------------------------------------------------------------------------------
    protected void OnTriggerEnter(Collider other)
    {
        if (!_autoOpen || !_isClosed) return;

        bool haveRequiredStates = true;
        if(_requiredStates.Count > 0)
        {
            if (ApplicationManager.instance == null) haveRequiredStates = false;
            else
                haveRequiredStates = ApplicationManager.instance.AreStatesSet(_requiredStates);
        }

        // Only activate if all requirement is met
        if(haveRequiredStates && HaveRequiredInvItems())
        {
            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = Activate(_plane.GetSide(other.transform.position));
            StartCoroutine(_coroutine);
        }
    }
}
