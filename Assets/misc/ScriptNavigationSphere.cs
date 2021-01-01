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

	private UnityEngine.UI.Text objPosInfo;

    private Vector3 posNavSphereStartLeft;
    private Vector3 posNavSphereStartRight;

    // ezek az értékek a chaperone-hoz viszonyítottan kellenek
    private Vector3 posNavSphereCenterStart;
    private Vector3 dirNavSphereStartForwardNorm;
    private Vector3 dirNavSphereStartRightNorm;
    private Vector3 dirNavSphereStartUpNorm;
    private Quaternion quatNavSphereCenterStart;

    // ezek world coord értékek
    private Vector3 posChaperonedAtStart;
    private Quaternion quatChaperonedAtStart;
    private Vector3 posLeftHandAtStart;
    private Quaternion quatLeftHanddAtStart;
    private float magnitudeNavSphereStart;

    private Vector3 anglesNavSphere;
    private float velocityForward = 0f;
    private float velocityUp = 0f;
    private float velocityRight = 0f;
    private Vector3 posRotDiff;

    private int status = 0;
    private int countUpdates = 0;

    // Update is called once per frame
    void Update()
    {
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

        this.posChaperonedAtStart = this.transform.position;
        this.posLeftHandAtStart = Player.instance.leftHand.transform.position;

        this.quatChaperonedAtStart = this.transform.rotation;
        this.quatLeftHanddAtStart = Player.instance.leftHand.transform.rotation;
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

        Vector3 diffChaperonePos = this.transform.position - this.posChaperonedAtStart;
        Vector3 diffLeftHandPos = Player.instance.leftHand.transform.position - this.posLeftHandAtStart - diffChaperonePos;

        Quaternion diffChaperoneQuat = Quaternion.Inverse( this.quatChaperonedAtStart ) * this.transform.rotation;
        Quaternion diffLeftHandQuatTmp1 = Quaternion.Inverse( this.quatLeftHanddAtStart ) * Player.instance.leftHand.transform.rotation;
        Quaternion diffLeftHandQuat = Quaternion.Inverse( diffChaperoneQuat ) * diffLeftHandQuatTmp1;

        this.anglesNavSphere = diffLeftHandQuat.eulerAngles;

        Quaternion quatTmp2 = Quaternion.Lerp( Quaternion.identity,diffLeftHandQuat,deltaTime );

        Vector3 vecHmdTranslation = this.transform.position - Player.instance.hmdTransform.position;
        Matrix4x4 m4rot = Matrix4x4.Rotate( quatTmp2 );
        Matrix4x4 m4trans1 = Matrix4x4.Translate( vecHmdTranslation );
        Matrix4x4 m4trans2 = Matrix4x4.Translate( -vecHmdTranslation );

        Matrix4x4 m4full = (m4trans1 * m4rot) * m4trans2;

        this.transform.rotation *= m4full.rotation;
        this.transform.position += m4full.GetPosition();
        //this.posRotDiff = (quatTmp2 * (-Player.instance.hmdTransform.position)) + Player.instance.hmdTransform.position;
        //this.transform.position += ( this.posRotDiff-Player.instance.hmdTransform.position );
    }

    void NavUpdatePositionAndRotationOld()
    {
        countUpdates++;
        // az irányításhoz az kell, hogy a chaperone-hoz viszonyítva kapjam meg, így tudom csak jól számolni a gyorsulás értékeket, még ha körbenézek is
        //if ( inputNavSphereLeftStart.state==true && inputNavSphereRightStart.state==true )
        if ( inputNavSphereLeftStart.state==true || inputNavSphereRightStart.state==true )
        {
            if ( this.status==0 )
                countUpdates = 0;
            else
                countUpdates++;

            Vector3 posNavSphereLeft = Player.instance.leftHand.transform.position - this.transform.position;
            Vector3 posNavSphereRight = Player.instance.rightHand.transform.position - this.transform.position;
            Vector3 posNavSphereCenter = ( posNavSphereLeft + posNavSphereRight )/2;
            Vector3 diffLeftToRight = posNavSphereRight - posNavSphereLeft;
            Vector3 diffLeftToRightNorm = diffLeftToRight.normalized;
            /*
            if ( countUpdates==0 )
            {
                this.magnitudeNavSphereStart = diffLeftToRight.magnitude;
                posNavSphereLeft = new Vector3( posNavSphereCenter.x-this.magnitudeNavSphereStart/2,posNavSphereCenter.y,posNavSphereCenter.z );
                posNavSphereRight = new Vector3( posNavSphereCenter.x+this.magnitudeNavSphereStart/2,posNavSphereCenter.y,posNavSphereCenter.z );
            }
            if ( countUpdates>0 && countUpdates<100 )
            {
                posNavSphereLeft = this.posNavSphereStartLeft;
                posNavSphereRight = this.posNavSphereStartRight;
            }
            if ( countUpdates>=100 && countUpdates<2000 )
            {
                posNavSphereLeft = this.posNavSphereStartLeft;
                posNavSphereRight = this.posNavSphereStartRight;
                posNavSphereLeft.z = this.posNavSphereStartLeft.z + 0.1f;
                posNavSphereRight.z = this.posNavSphereStartRight.z - 0.1f;
            }
            if ( countUpdates>=2000 )
            {
                posNavSphereLeft = this.posNavSphereStartLeft;
                posNavSphereRight = this.posNavSphereStartRight;
            }
            posNavSphereCenter = ( posNavSphereLeft + posNavSphereRight )/2;
            diffLeftToRight = posNavSphereRight - posNavSphereLeft;
            diffLeftToRightNorm = diffLeftToRight.normalized;
            */

            if ( this.status==0 )
            {
                this.magnitudeNavSphereStart = diffLeftToRight.magnitude;

                this.posNavSphereStartLeft = posNavSphereLeft;
                this.posNavSphereStartRight = posNavSphereRight;

                this.posNavSphereCenterStart = posNavSphereCenter;
                //quatNavSphereCenterStart = Player.instance.hmdTransform.rotation;
                // merőleges kell a navSphere két széle közti egyenesre és a felfele irányra
                // ezt vektoriális szorzással számítom ki: dirQuat
                // https://docs.unity3d.com/ScriptReference/Vector3.Cross.html
                // quaternion-t dirQuat-ból és 0 fokos szögből számolom
                this.dirNavSphereStartForwardNorm = Vector3.Cross( diffLeftToRight,Vector3.up ).normalized;
                this.dirNavSphereStartRightNorm = diffLeftToRightNorm;
                this.dirNavSphereStartUpNorm = Vector3.Cross( this.dirNavSphereStartForwardNorm,this.dirNavSphereStartRightNorm ).normalized;
                this.quatNavSphereCenterStart =  Quaternion.LookRotation( this.dirNavSphereStartForwardNorm,this.dirNavSphereStartUpNorm ).normalized;
                this.status = 1;

                Debug.Log( string.Format( "posNavSphereLeft ({0},{1},{2})",posNavSphereLeft.x,posNavSphereLeft.y,posNavSphereLeft.z ) );
                Debug.Log( string.Format( "posNavSphereRight ({0},{1},{2})",posNavSphereRight.x,posNavSphereRight.y,posNavSphereRight.z ) );

                Debug.Log( string.Format( "posNavSphereStartLeft ({0},{1},{2})",this.posNavSphereStartLeft.x,this.posNavSphereStartLeft.y,this.posNavSphereStartLeft.z ) );
                Debug.Log( string.Format( "posNavSphereStartRight ({0},{1},{2})",this.posNavSphereStartRight.x,this.posNavSphereStartRight.y,this.posNavSphereStartRight.z ) );
            }
            else
            {
                float deltaTime = Time.deltaTime/10f;

                Vector3 diffCenter = posNavSphereCenter - this.posNavSphereCenterStart;
                // dot product kell ahhoz, hogy egy vektor adott irányú komponensét határozzam meg
                //Vector3 diffForward = this.dirNavSphereStartForwardNorm * Vector3.Dot( this.dirNavSphereStartForwardNorm,diffCenter );
                //Vector3 diffRight = this.dirNavSphereStartRightNorm * Vector3.Dot( this.dirNavSphereStartRightNorm,diffCenter );
                //Vector3 diffUp = this.dirNavSphereStartUpNorm * Vector3.Dot( this.dirNavSphereStartUpNorm,diffCenter );
                this.velocityForward = Vector3.Dot( this.dirNavSphereStartForwardNorm,diffCenter );
                this.velocityUp = Vector3.Dot( this.dirNavSphereStartUpNorm,diffCenter );
                this.velocityRight = Vector3.Dot( this.dirNavSphereStartRightNorm,diffCenter );

                // 2 quaternion diff-jét kell kiszámolni, majd ezzel kell megszorozni a hmd rotation-jét
                // https://forum.unity.com/threads/get-the-difference-between-two-quaternions-and-add-it-to-another-quaternion.513187/
                Vector3 dirNavSphereForwardNorm = Vector3.Cross( diffLeftToRight,Vector3.up ).normalized;
                Vector3 dirNavSphereUpNorm = Vector3.Cross( dirNavSphereForwardNorm,diffLeftToRightNorm ).normalized;
                Quaternion quatNavSphere = Quaternion.LookRotation( dirNavSphereForwardNorm,dirNavSphereUpNorm ).normalized;
                //Quaternion quatTmp = this.quatNavSphereCenterStart * Quaternion.Inverse( quatNavSphere );

                //if ( countUpdates%100==0 ) Debug.Log( string.Format( "dirNavSphereUpNorm ({0},{1},{2})",dirNavSphereUpNorm.x,dirNavSphereUpNorm.y,dirNavSphereUpNorm.z ) );

                // ez is van: https://answers.unity.com/questions/35541/problem-finding-relative-rotation-from-one-quatern.html                
                Quaternion quatTmp1 = Quaternion.Inverse( this.quatNavSphereCenterStart ) * quatNavSphere;
                //if ( countUpdates%50==0 ) Debug.Log( string.Format( "quatTmp1 ({0},{1},{2},{3})",quatTmp1.x,quatTmp1.y,quatTmp1.z,quatTmp1.w ) );
                this.anglesNavSphere = quatNavSphere.eulerAngles;

                Quaternion quatTmp2 = Quaternion.Lerp( Quaternion.identity,quatTmp1,deltaTime );
                //if ( countUpdates%50==0 ) Debug.Log( string.Format( "quatTmp2 ({0},{1},{2},{3})",quatTmp2.x,quatTmp2.y,quatTmp2.z,quatTmp2.w ) );

                //this.transform.rotation *= quatTmp2;
                //if ( countUpdates%50==0 ) Debug.Log( string.Format( "rotation ({0},{1},{2},{3})",this.transform.rotation.x,this.transform.rotation.y,this.transform.rotation.z,this.transform.rotation.w ) );

                //this.transform.position += quatTmp2 * (-Player.instance.hmdTransform.position);

                Vector3 vecForward = this.transform.rotation * Vector3.forward;
                Vector3 vecUp = this.transform.rotation * Vector3.up;
                Vector3 vecRight = this.transform.rotation * Vector3.right;

                this.transform.position += vecForward * velocityForward * deltaTime;
                this.transform.position += vecUp * velocityUp * deltaTime;
                this.transform.position += vecRight * velocityRight * deltaTime;
            }
        }
        else
        {
            this.status = 0;
        }
        updateHUDPosInfo();
    }

	private void updateHUDPosInfo()
	{
		if ( objPosInfo==null )
		{
			objPosInfo = GameObject.FindGameObjectWithTag( "hudText" ).GetComponent<UnityEngine.UI.Text>();
		}

        Vector3 hmdTransform = Player.instance.hmdTransform.position;
        Vector3 hmdRotation = Player.instance.hmdTransform.rotation.eulerAngles;
        Vector3 chpTransform = this.transform.position;
        Vector3 chpRotation = this.transform.rotation.eulerAngles;
        Vector3 lhandTransform = Player.instance.leftHand.transform.position;
        Vector3 lhandRotation = Player.instance.leftHand.transform.rotation.eulerAngles;

		objPosInfo.text = string.Format( 
			"counter:{0}\n" + 
			"position:{1,0:F2},{2,0:F2},{3,0:F2}\n" +
			"anglesNavSphere: {4,0:F6} {5,0:F6} {6,0:F6}\n" +
			"velocity f:{7,0:F6} u:{8,0:F6} r:{9,0:F6}\n" +
            "hmdPos:{10,0:F6},{11,0:F6},{12,0:F6}\n" +
            "hmdRot:{13,0:F6},{14,0:F6},{15,0:F6}\n" +
            "chpPos:{16,0:F6},{17,0:F6},{18,0:F6}\n" +
            "chpRot:{19,0:F6},{20,0:F6},{21,0:F6}\n" +
            "lhandPos:{22,0:F6},{23,0:F6},{24,0:F6}\n" +
            "lhandRot:{25,0:F6},{26,0:F6},{27,0:F6}\n" +
            "posRotDiff:{28,0:F6},{29,0:F6},{30,0:F6}\n",
			countUpdates,
			transform.position.x,transform.position.y,transform.position.z,
            anglesNavSphere.x,anglesNavSphere.y,anglesNavSphere.z,
            velocityForward,velocityUp,velocityRight,
            hmdTransform.x,hmdTransform.y,hmdTransform.z,
            hmdRotation.x,hmdRotation.y,hmdRotation.z,
            chpTransform.x,chpTransform.y,chpTransform.z,
            chpRotation.x,chpRotation.y,chpRotation.z,
            lhandTransform.x,lhandTransform.y,lhandTransform.z,
            lhandRotation.x,lhandRotation.y,lhandRotation.z,
            posRotDiff.x,posRotDiff.y,posRotDiff.z );
	}
}
