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
    public GameObject gameObjRightHand;
    public GameObject gameObjHmd;

	public UnityEngine.UI.Text objPosInfo;

    private float VELOCITY_MULT1 = 1f;
    //private float MAX_ACCELERATION = 0.2f;
    private float MAX_VELOCITY = 20f;
    private float ACCELERATION_MULT1 = 0.8f;
    private float ACCELERATION_STOP_MULT2 = 2f;
    private float ANGULAR_VELOCITY_MULT = 0.8f;
    private float TRANSLATION_DEADZONE = 0.02f;
    private float TRANSLATION_POW_MULT1 = 25f;

    // ezek world coord értékek
    private Vector3 posLeftHandRelativeToChaperoneAtStart;
    private Quaternion quatRightHandRelativeToChaperoneAtStart;
    //private float magnitudeNavSphereStart = 1.0f;   // jelenleg fix

    private Vector3 currentTranslationAsVelocity;
    private Vector3 savedTargetTranslationAsVelocityAtTrigger;
    private Vector3 targetTranslationAsVelocity;
    private Quaternion currentRotationAsAngleVelocity;
    private float translationPosMulti;
/*
    private float debugVelocityForward = 0f;
    private float debugVelocityUp = 0f;
    private float debugVelocityRight = 0f;
*/
    private int status = 0;
    private bool stopRequested = false;
    private int countUpdates = 0;


    // Start is called before the first frame update
    void Start()
    {
        this.currentTranslationAsVelocity = Vector3.zero;
        this.savedTargetTranslationAsVelocityAtTrigger = Vector3.zero;
        this.targetTranslationAsVelocity = Vector3.zero;
        this.currentRotationAsAngleVelocity = Quaternion.identity;
        this.translationPosMulti = TRANSLATION_POW_MULT1;
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
        if ( inputNavSphereLeftStart.state==true )
        {
            this.posLeftHandRelativeToChaperoneAtStart = this.gameObjLeftHand.transform.localPosition;
            this.savedTargetTranslationAsVelocityAtTrigger = this.targetTranslationAsVelocity;
            // csak az elején a triggerkor állítom
            CalcTranslationPosMulti();
        }
        if ( inputNavSphereRightStart.state==true )
            this.quatRightHandRelativeToChaperoneAtStart = this.gameObjRightHand.transform.localRotation;
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

        if ( inputNavSphereRightStart.state==true )
        {
            Vector3 savedHdmWorldPos = this.gameObjHmd.transform.position;

            // először kiszámolom a chaperone-hoz viszonyított rotációt
            Quaternion quatTmp1 = Quaternion.Inverse( this.quatRightHandRelativeToChaperoneAtStart ) * this.gameObjRightHand.transform.localRotation;

            // beállítom a sebességet
            this.currentRotationAsAngleVelocity = Quaternion.Lerp( this.currentRotationAsAngleVelocity,quatTmp1,deltaTime * ANGULAR_VELOCITY_MULT );

            // majd ehhez képest a deltaTime rotiációt
            Quaternion quatTmp2 = Quaternion.Lerp( Quaternion.identity,this.currentRotationAsAngleVelocity,deltaTime );

            // végül módosítom a chaperone rotációját, és pozícióját
            Quaternion quatTmp3 = this.transform.rotation * quatTmp2;
            this.transform.rotation = quatTmp3.normalized;

            // kiszámolom az új rotációval a chp-ben a hmd helyzetét és eltolom a chp-t úgy, hogy a hmd 1 helyben kell maradjon
            //Vector3 newHdmWorldPos = this.transform.position + this.transform.rotation * this.gameObjHmd.transform.localPosition;
            Vector3 newHdmWorldPos = this.gameObjHmd.transform.position;
            this.transform.position -= ( newHdmWorldPos - savedHdmWorldPos );
        }

        if ( inputNavSphereLeftStart.state==true )
        {
            // transzláció kezelés
            // először kiszámolom a chaperone-hoz viszonyított transzlációt, minusz a default érték
            Vector3 diffLeftHandChaperone = this.gameObjLeftHand.transform.localPosition - this.posLeftHandRelativeToChaperoneAtStart;
            float diffLen = diffLeftHandChaperone.magnitude;
            if ( diffLen<TRANSLATION_DEADZONE )
                diffLen = 0;
            if ( diffLen>1 )
                diffLen = 1;
            float accLen = Mathf.Pow( 2f,(diffLen-TRANSLATION_DEADZONE)*this.translationPosMulti )-1f;
            Vector3 targetAddTranslationAsVelocity = diffLeftHandChaperone.normalized * accLen;
            this.targetTranslationAsVelocity = targetAddTranslationAsVelocity + this.savedTargetTranslationAsVelocityAtTrigger;

            float targetVelotityLen  = this.targetTranslationAsVelocity.magnitude;
            if ( targetVelotityLen>MAX_VELOCITY )
            {
                this.targetTranslationAsVelocity = this.targetTranslationAsVelocity.normalized * MAX_VELOCITY;
            }
        }
        ModifyCurrentVelocity( deltaTime );
        this.transform.position += (this.transform.rotation * this.currentTranslationAsVelocity) * (VELOCITY_MULT1*deltaTime);
    }

    private void CalcTranslationPosMulti()
    {
        float diffTwoHands = (this.gameObjLeftHand.transform.localPosition - this.gameObjRightHand.transform.localPosition).magnitude;
        // ha 10 cm-n belül van a 2 kéz, akkor a minimum 2 az érték
        // ha 50 m-ig fokozatosan nő a max 25-re az érték
        if ( diffTwoHands<=0.1f ) this.translationPosMulti = 2f;
        else if ( diffTwoHands>0.5f ) this.translationPosMulti = 25f;
        else
        {
            this.translationPosMulti = 2f + 23f*( (diffTwoHands-0.1f)/0.4f );
        }
    }

    private void ModifyCurrentVelocity( float deltaTime )
    {
        Vector3 diffVelocity = this.targetTranslationAsVelocity - this.currentTranslationAsVelocity;
        Vector3 diffVelocityNorm = diffVelocity.normalized;
        float diffLen = diffVelocity.magnitude;
        if ( diffLen<0.01f )
        {
            this.currentTranslationAsVelocity = this.targetTranslationAsVelocity;
        }
        else
        {
            if ( diffLen>1f )
                diffLen = 1;
            float addVelocityLen = diffLen * deltaTime;
            if ( this.stopRequested==true )
            {
                addVelocityLen = addVelocityLen * ACCELERATION_STOP_MULT2;
                //Debug.Log( String.Format( "addVelocityLen:{0} diffLen:{1} {2}",addVelocityLen,diffLen,this.currentTranslationAsVelocity.x ) );
            }
            else
                addVelocityLen *= ACCELERATION_MULT1;

            this.currentTranslationAsVelocity += diffVelocityNorm * addVelocityLen;
        }
        if ( this.stopRequested==true && this.currentTranslationAsVelocity.magnitude<0.02 )
        {
            this.currentTranslationAsVelocity = Vector3.zero;
            this.stopRequested = false;
        }
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
        ModifyCurrentVelocity( deltaTime );
        this.transform.position += (this.transform.rotation * this.currentTranslationAsVelocity) * (VELOCITY_MULT1*deltaTime);
    }

    void ProcessInputStopMovements()
    {
        if ( inputStopForwardMovement.state==true || inputStopSideMovement.state==true || inputStopUpDownMovement.state==true || inputStopAllMovement.state==true )
        {
            //this.currentTranslationAsVelocity = Vector3.zero;
            this.currentRotationAsAngleVelocity = Quaternion.identity;
            this.targetTranslationAsVelocity = Vector3.zero;
            this.stopRequested = true;
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
/*
        Vector3 pos1 = this.gameObjLeftHand.transform.localPosition;
        Vector3 pos2 = this.gameObjHmd.transform.localPosition;
        Vector3 pos3 = pos1 - this.posLeftHandRelativeToChaperoneAtStart;

        Vector3 hmdTransform = this.gameObjHmd.transform.position;
        Vector3 hmdRotation = this.gameObjHmd.transform.rotation.eulerAngles;
        Vector3 chpTransform = this.transform.position;
        Vector3 chpRotation = this.transform.rotation.eulerAngles;
        Vector3 lhandTransform = this.gameObjLeftHand.transform.position;
        Vector3 lhandRotation = this.gameObjLeftHand.transform.rotation.eulerAngles;
*/
        this.indexFormat = 0;
        this.strFormat = "";
        addObj( "counter:{{{0}}}\n",countUpdates );
        addVector3( "position:{{{0},0:F2}} {{{1},0:F2}} {{{2},0:F2}}\n",this.transform.position );
        addVector3( "rotation:{{{0},0:F6}} {{{1},0:F6}} {{{2},0:F6}}\n",this.currentRotationAsAngleVelocity.eulerAngles );
        addVector3( "velocity:{{{0},0:F6}} {{{1},0:F6}} {{{2},0:F6}}\n",this.currentTranslationAsVelocity );
        addVector3( "targetVelocity:{{{0},0:F6}} {{{1},0:F6}} {{{2},0:F6}}\n",this.targetTranslationAsVelocity );
        addObj( "translationPosMulti:{{{0},0:F2}}\n",this.translationPosMulti );
        addObj( "velocityMagnitude:{{{0},0:F6}}\n",this.currentTranslationAsVelocity.magnitude );
        addObj( "velocityMagnitudeSqrt:{{{0},0:F6}}\n",this.currentTranslationAsVelocity.sqrMagnitude );

        //Debug.Log( this.strFormat );
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
        this.objsFormat[this.indexFormat] = vec.x;
        this.objsFormat[this.indexFormat+1] = vec.y;
        this.objsFormat[this.indexFormat+2] = vec.z;
        this.indexFormat += 3;
    }
}
