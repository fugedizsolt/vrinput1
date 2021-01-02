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

    public GameObject gameObjLeftHand;
    public GameObject gameObjHmd;

	private UnityEngine.UI.Text objPosInfo;

    // ezek world coord értékek
    private Vector3 posLeftHandRelativeToChaperoneAtStart;
    private Quaternion quatLeftHandRelativeToChaperoneAtStart;
    private float magnitudeNavSphereStart = 1.0f;   // jelenleg fix

    private Vector3 debugDiffPosLeftHandFromStart;
    private Vector3 debugDiffAnglesLeftHandFromStart;
    private float debugVelocityForward = 0f;
    private float debugVelocityUp = 0f;
    private float debugVelocityRight = 0f;

    private int status = 0;
    private int countUpdates = 0;

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
                NavUpdatePositionAndRotation();
            }
        }
        else
        {
            NavEnd();
        }
        updateHUDPosInfo();
    }

    private void NavStart()
    {
        countUpdates = 0;

        // start esetén eltárolom a chaperone-hoz relatív position-t és rotation-t
        this.posLeftHandRelativeToChaperoneAtStart = MyInverseTransformPoint( this.transform,this.gameObjLeftHand.transform.position );
        this.quatLeftHandRelativeToChaperoneAtStart =  Quaternion.Inverse( this.transform.rotation ) * this.gameObjLeftHand.transform.rotation;
    }

    private void NavEnd()
    {
        this.status = 0;
    }

    private bool IsNavStarted()
    {
        return ( inputNavSphereLeftStart.state==true || inputNavSphereRightStart.state==true );
    }

    private bool IsNavTriggered()
    {
        return ( this.status==0 );
    }

    void NavUpdatePositionAndRotation()
    {
        float deltaTime = Time.deltaTime;

        // először kiszámolom a chaperone-hoz relatív position-t és rotation-t
        Vector3 currentRelativePosDiffLeftHandChaperone = MyInverseTransformPoint( this.transform,this.gameObjLeftHand.transform.position );
        Quaternion currentRelativeQuatDiffLeftHandChaperone = Quaternion.Inverse( this.transform.rotation ) * this.gameObjLeftHand.transform.rotation;

        // transzláció számolása:
        Vector3 diffLeftHandChaperone = currentRelativePosDiffLeftHandChaperone - this.posLeftHandRelativeToChaperoneAtStart;
        Vector3 diffLeftHandChaperoneLerp = diffLeftHandChaperone * deltaTime;

        // rotáció számolása:
        //   kiszámolom az aktuális és a startnál tárolt rotation közti különbséget
        //Quaternion quatTmp1 = Quaternion.Inverse( this.quatLeftHandRelativeToChaperoneAtStart ) * currentRelativeQuatDiffLeftHandChaperone;
        //Quaternion quatTmp1 = Quaternion.Euler( diffLeftHandChaperone.x*40,diffLeftHandChaperone.y*40,diffLeftHandChaperone.z*40 );
        Quaternion quatTmp1 = Quaternion.Euler( 0,0,diffLeftHandChaperone.x*100 );
        //   ehhez képest a deltaTime rotiációt
        Quaternion quatTmp2 = Quaternion.Lerp( Quaternion.identity,quatTmp1,deltaTime );

        // ezután meg kell határozni azt a chaperone eltolást(transzlációt), amely a hmd-ben történő rotáció miatt éri a chaperone-t
        Vector3 diffHmdChaperone = MyInverseTransformPoint( this.transform,this.gameObjHmd.transform.position );
        Vector3 diffRotHmdChaperone = quatTmp2 * diffHmdChaperone - diffHmdChaperone;

        this.transform.rotation *= quatTmp2;
        this.transform.position += diffRotHmdChaperone;

        Vector3 vecAdd = this.transform.rotation * diffLeftHandChaperoneLerp;
        //this.transform.position += vecAdd;

        this.debugDiffPosLeftHandFromStart = diffLeftHandChaperone;
        this.debugDiffAnglesLeftHandFromStart = quatTmp1.eulerAngles;
    }

    private Vector3 MyInverseTransformPoint( Transform transform,Vector3 worldCoordPos )
    {
        Vector3 diff = ( worldCoordPos - transform.position );
        return Quaternion.Inverse( transform.rotation ) * diff;
    }

    private int indexFormat = 0;
    private string strFormat;
    private object[] objsFormat = new object[100];
	private void updateHUDPosInfo()
	{
		if ( objPosInfo==null )
		{
			objPosInfo = GameObject.FindGameObjectWithTag( "hudText" ).GetComponent<UnityEngine.UI.Text>();
		}

        Vector3 pos1 = MyInverseTransformPoint( this.transform,this.gameObjLeftHand.transform.position );
        Vector3 pos2 = MyInverseTransformPoint( this.transform,this.gameObjHmd.transform.position );
        Vector3 pos3 = MyInverseTransformPoint( this.gameObjLeftHand.transform,this.gameObjHmd.transform.position );
        Vector3 pos4 = pos1 - this.posLeftHandRelativeToChaperoneAtStart;

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
        addVector3( "invChpHand:{{{0},0:F6}} {{{1},0:F6}} {{{2},0:F6}}\n",pos1 );
        addVector3( "invChpHmd:{{{0},0:F6}} {{{1},0:F6}} {{{2},0:F6}}\n",pos2 );
        addVector3( "invHandHmd:{{{0},0:F6}} {{{1},0:F6}} {{{2},0:F6}}\n",pos3 );
        addObj( "diffHandChpX:{{{0}}}\n",pos4.x*100 );
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
