using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class ScriptNavigationSphere : MonoBehaviour
{
    public SteamVR_Action_Boolean inputNavSphereLeftStart;
    public SteamVR_Action_Boolean inputNavSphereRightStart;
    public SteamVR_Action_Boolean inputStopForwardMovement;
    public SteamVR_Action_Boolean inputStopSideMovement;
    public SteamVR_Action_Boolean inputStopUpDownMovement;
    public SteamVR_Action_Boolean inputStopAllMovement;

    public GameObject gameObjLeftHand;
    public GameObject gameObjHmd;

	public UnityEngine.UI.Text objPosInfo;

    public float VELOCITY_MULT1 = 1f;
    public float MAX_ACCELERATION = 0.2f;
    public float ACCELERATION_MULT1 = 0.2f;
    public float ANGULAR_VELOCITY_MULT = 1.5f;
    public float TRANSLATION_DEADZONE = 0.02f;
    public float TRANSLATION_POW_MULT1 = 10f;

    // ezek world coord értékek
    private Vector3 posLeftHandRelativeToChaperoneAtStart;
    private Quaternion quatLeftHandRelativeToChaperoneAtStart;
    private float magnitudeNavSphereStart = 1.0f;   // jelenleg fix

    private Vector3 currentTranslationAsVelocity;
    private Vector3 currentTranslationAcceleration;
    private Vector3 targetTranslationAcceleration;
    private Quaternion currentRotationAsAngleVelocity;

    private Vector3 debugDiffPosLeftHandFromStart;
    private Vector3 debugDiffAnglesLeftHandFromStart;
    private float debugVelocityForward = 0f;
    private float debugVelocityUp = 0f;
    private float debugVelocityRight = 0f;

    private int status = 0;
    private int countUpdates = 0;


    // Start is called before the first frame update
    void Start()
    {
        this.currentTranslationAsVelocity = Vector3.zero;
        this.currentTranslationAcceleration = Vector3.zero;
        this.targetTranslationAcceleration = Vector3.zero;
        this.currentRotationAsAngleVelocity = Quaternion.identity;
    }

    // Update is called once per frame
    void Update()
    {
        //this.transform.position -= new Vector3( 0,0,1 )/100f;
        if ( IsNavTriggered()==true )
        {
            if ( IsNavStarted()==false )
            {
                NavStart();
            }
            else
            {
                NavUpdatePositionAndRotationWithAcceleration();
            }
        }
        else
        {
            if ( IsNavStarted()==true )
                NavEnd();
            ProcessInputStopMovements();
            NavUpdatePositionAndRotationWithoutAcceleration();
        }
        updateHUDPosInfo();
    }

    private void NavStart()
    {
        this.status = 1;
        countUpdates = 0;

        // start esetén eltárolom a chaperone-hoz relatív position-t és rotation-t
        this.posLeftHandRelativeToChaperoneAtStart = this.gameObjLeftHand.transform.localPosition;
        this.quatLeftHandRelativeToChaperoneAtStart = this.gameObjLeftHand.transform.localRotation;
    }

    private void NavEnd()
    {
        this.status = 0;
    }

    private bool IsNavTriggered()
    {
        return ( inputNavSphereLeftStart.state==true || inputNavSphereRightStart.state==true );
    }

    private bool IsNavStarted()
    {
        return ( this.status==1 );
    }

    void NavUpdatePositionAndRotationWithAcceleration()
    {
        float deltaTime = Time.deltaTime;

        // először kiszámolom a chaperone-hoz viszonyított rotációt
        Quaternion quatTmp1 = Quaternion.Inverse( this.quatLeftHandRelativeToChaperoneAtStart ) * this.gameObjLeftHand.transform.localRotation;

        // beállítom a sebességet
        this.currentRotationAsAngleVelocity = Quaternion.Lerp( this.currentRotationAsAngleVelocity,quatTmp1,deltaTime * ANGULAR_VELOCITY_MULT );

        // majd ehhez képest a deltaTime rotiációt
        Quaternion quatTmp2 = Quaternion.Lerp( Quaternion.identity,this.currentRotationAsAngleVelocity,deltaTime );

        Vector3 savedHdmWorldPos = this.gameObjHmd.transform.position;

        // végül módosítom a chaperone rotációját, és pozícióját
        Quaternion quatTmp3 = this.transform.rotation * quatTmp2;
        this.transform.rotation = quatTmp3.normalized;

        // kiszámolom az új rotációval a chp-ben a hmd helyzetét és eltolom a chp-t úgy, hogy a hmd 1 helyben kell maradjon
        //Vector3 newHdmWorldPos = this.transform.position + this.transform.rotation * this.gameObjHmd.transform.localPosition;
        Vector3 newHdmWorldPos = this.gameObjHmd.transform.position;
        this.transform.position -= ( newHdmWorldPos - savedHdmWorldPos );

        // transzláció kezelés
        // először kiszámolom a chaperone-hoz viszonyított transzlációt, minusz a default érték
        Vector3 diffLeftHandChaperone = this.gameObjLeftHand.transform.localPosition - this.posLeftHandRelativeToChaperoneAtStart;
        float diffLen = diffLeftHandChaperone.magnitude;
        if ( diffLen<TRANSLATION_DEADZONE )
            diffLen = 0;
        if ( diffLen>8 )
            diffLen = 8;
        float accLen = Mathf.Pow( 2f,diffLen*TRANSLATION_POW_MULT1 )-1f;
        this.targetTranslationAcceleration = diffLeftHandChaperone.normalized * accLen;
        Vector3 diffAcceleration = this.targetTranslationAcceleration - this.currentTranslationAcceleration;
        Vector3 addVelocity = diffAcceleration * (deltaTime * ACCELERATION_MULT1);
        if ( addVelocity.magnitude<0.01f )
            addVelocity = addVelocity.normalized * 0.01f;
        this.currentTranslationAcceleration += addVelocity;

        float currentAccelerationVectorLength = this.currentTranslationAcceleration.magnitude;
        float targetAccelerationVectorLength = this.targetTranslationAcceleration.magnitude;
        if ( currentAccelerationVectorLength>targetAccelerationVectorLength )
        {
            this.currentTranslationAcceleration = this.targetTranslationAcceleration;
            currentAccelerationVectorLength = targetAccelerationVectorLength;
        }

        if ( currentAccelerationVectorLength>MAX_ACCELERATION )
        {
            this.currentTranslationAcceleration = this.currentTranslationAcceleration.normalized * MAX_ACCELERATION;
        }
        this.currentTranslationAsVelocity += this.currentTranslationAcceleration * (deltaTime);
        this.transform.position += (this.transform.rotation * this.currentTranslationAsVelocity) * (VELOCITY_MULT1*deltaTime);

        this.debugDiffPosLeftHandFromStart = diffLeftHandChaperone;
        this.debugDiffAnglesLeftHandFromStart = quatTmp1.eulerAngles;
    }

    void NavUpdatePositionAndRotationWithoutAcceleration()
    {
        float deltaTime = Time.deltaTime;

        // majd ehhez képest a deltaTime rotiációt
        Quaternion quatTmp2 = Quaternion.Lerp( Quaternion.identity,this.currentRotationAsAngleVelocity,deltaTime );

        Vector3 savedHdmWorldPos = this.gameObjHmd.transform.position;

        // végül módosítom a chaperone rotációját, és pozícióját
        Quaternion quatTmp3 = this.transform.rotation * quatTmp2;
        this.transform.rotation = quatTmp3.normalized;

        // kiszámolom az új rotációval a chp-ben a hmd helyzetét és eltolom a chp-t úgy, hogy a hmd 1 helyben kell maradjon
        //Vector3 newHdmWorldPos = this.transform.position + this.transform.rotation * this.gameObjHmd.transform.localPosition;
        Vector3 newHdmWorldPos = this.gameObjHmd.transform.position;
        this.transform.position -= ( newHdmWorldPos - savedHdmWorldPos );

        // transzláció kezelés
        // először kiszámolom a chaperone-hoz viszonyított transzlációt, minusz a default érték
        this.targetTranslationAcceleration = Vector3.zero;
        this.currentTranslationAcceleration = Vector3.zero;
        this.transform.position += (this.transform.rotation * this.currentTranslationAsVelocity) * (VELOCITY_MULT1*deltaTime);

        this.debugDiffPosLeftHandFromStart = Vector3.zero;
        this.debugDiffAnglesLeftHandFromStart = this.currentRotationAsAngleVelocity.eulerAngles;
    }

    void ProcessInputStopMovements()
    {
        if ( inputStopForwardMovement.state==true || inputStopSideMovement.state==true || inputStopUpDownMovement.state==true || inputStopAllMovement.state==true )
        {
            this.currentTranslationAsVelocity = Vector3.zero;
        }
    }
/*
    private Vector3 MyInverseTransformPoint( Transform transform,Vector3 worldCoordPos )
    {
        Vector3 diff = ( worldCoordPos - transform.position );
        return Quaternion.Inverse( transform.rotation ) * diff;
    }
*/
    private int indexFormat = 0;
    private string strFormat;
    private object[] objsFormat = new object[100];
	private void updateHUDPosInfo()
	{
        Vector3 pos1 = this.gameObjLeftHand.transform.localPosition;
        Vector3 pos2 = this.gameObjHmd.transform.localPosition;
        Vector3 pos3 = pos1 - this.posLeftHandRelativeToChaperoneAtStart;

        Vector3 hmdTransform = this.gameObjHmd.transform.position;
        Vector3 hmdRotation = this.gameObjHmd.transform.rotation.eulerAngles;
        Vector3 chpTransform = this.transform.position;
        Vector3 chpRotation = this.transform.rotation.eulerAngles;
        Vector3 lhandTransform = this.gameObjLeftHand.transform.position;
        Vector3 lhandRotation = this.gameObjLeftHand.transform.rotation.eulerAngles;

        this.indexFormat = 0;
        this.strFormat = "";
        addObj( "counter:{{{0}}}\n",countUpdates );
        addVector3( "position:{{{0},0:F2}} {{{1},0:F2}} {{{2},0:F2}}\n",this.transform.position );
        addVector3( "diffPos:{{{0},0:F6}} {{{1},0:F6}} {{{2},0:F6}}\n",this.debugDiffPosLeftHandFromStart );
        addVector3( "diffAngles:{{{0},0:F6}} {{{1},0:F6}} {{{2},0:F6}}\n",this.debugDiffAnglesLeftHandFromStart );
        addVector3( "velocity:{{{0},0:F6}} {{{1},0:F6}} {{{2},0:F6}}\n",this.currentTranslationAsVelocity );
        addVector3( "currentAcceleration:{{{0},0:F6}} {{{1},0:F6}} {{{2},0:F6}}\n",this.currentTranslationAcceleration );
        addVector3( "targetAcceleration:{{{0},0:F6}} {{{1},0:F6}} {{{2},0:F6}}\n",this.targetTranslationAcceleration );
        //addVector3( "invChpHand:{{{0},0:F6}} {{{1},0:F6}} {{{2},0:F6}}\n",pos1 );
        //addVector3( "invChpHmd:{{{0},0:F6}} {{{1},0:F6}} {{{2},0:F6}}\n",pos2 );
        //addObj( "diffHandChpX:{{{0}}}\n",pos3.x*100 );
        //addVector3( "diffHandChp:{{{0},0:F6}} {{{1},0:F6}} {{{2},0:F6}}\n",pos4 );

		objPosInfo.text = string.Format( this.strFormat,this.objsFormat );
	}

    private void addObj( string msg,object objVal )
    {
        this.strFormat += string.Format( msg,this.indexFormat );
        this.objsFormat[this.indexFormat] = objVal;
        this.indexFormat++;
    }
    private void addVector3( string msg,Vector3 vec )
    {
        this.strFormat += string.Format( msg,this.indexFormat,this.indexFormat+1,this.indexFormat+2 );
        Debug.Log( this.strFormat );
        this.objsFormat[this.indexFormat] = vec.x;
        this.objsFormat[this.indexFormat+1] = vec.y;
        this.objsFormat[this.indexFormat+2] = vec.z;
        this.indexFormat += 3;
    }
}
