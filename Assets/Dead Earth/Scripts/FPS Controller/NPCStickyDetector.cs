using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// ------------------------------------------------------------------------------------------------
// CLASS	:	NPCStickyDetector
// DESC		:	Should be added to a child game object of the FPS controller and be assigned
//				a layer which generates collision/trigger messages with the AI Entity Layer.
//				It instructs the FPS controller to apply stickiness and also override the
//				zombie's visual threat and current state
// -------------------------------------------------------------------------------------------------
public class NPCStickyDetector : MonoBehaviour 
{

	FPSController _controller = null;

	// Use this for initialization
	void Start () 
	{
		_controller = GetComponentInParent<FPSController>();	
	}

	void OnTriggerStay( Collider col )
	{
		// Is this collier a zombie
		AIStateMachine machine = GameSceneManager.instance.GetAIStateMachine( col.GetInstanceID());
		if (machine!=null && _controller!=null)
		{
			// Apply stickiness
			_controller.DoStickiness();

			// Set THIS location as the zombie's visual threat
			machine.VisualThreat.Set( AITargetType.Visual_Player,
									  _controller.characterController, 
									  _controller.transform.position, 
									  Vector3.Distance( machine.transform.position, _controller.transform.position)
									 );

			// Force the zombie in ATTACK state
			machine.SetStateOverride( AIStateType.Attack );

		}
	}

}
