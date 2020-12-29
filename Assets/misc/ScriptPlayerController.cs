using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class ScriptPlayerController : MonoBehaviour
{
    public SteamVR_Action_Vector2 input;
    public SteamVR_Action_Boolean inputJump;

    // Update is called once per frame
    void Update()
    {
        //Vector3 direction = Player.instance.hmdTransform
        Vector3 direction = Player.instance.hmdTransform.TransformDirection( new Vector3( input.axis.x,0,input.axis.y ) );
        Vector3 directionOnPlane = Vector3.ProjectOnPlane(direction, Vector3.up);
        transform.position += Time.deltaTime * directionOnPlane;


        if ( inputJump.stateDown==true )
        {
            Debug.Log( string.Format( "inputJump.stateDown before x:{0} directionOnPlane.x:{1}",transform.position.x,directionOnPlane.x ) );
            transform.position += new Vector3( 1,0,0 );
            Debug.Log( string.Format( "inputJump.stateDown after x:{0}",transform.position.x ) );
        }
    }
}
