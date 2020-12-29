using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class ScriptNavigationSphere : MonoBehaviour
{
    public SteamVR_Action_Boolean inputNavSphereLeftStart;
    public SteamVR_Action_Boolean inputNavSphereRightStart;

    private Vector3 posNavSphereStartLeft;
    private Vector3 posNavSphereStartRight;

    // ezek az értékek a chaperone-hoz viszonyítottan kellenek
    private Vector3 posNavSphereCenterStart;
    private Vector3 dirNavSphereStartForwardNorm;
    private Vector3 dirNavSphereStartRightNorm;
    private Vector3 dirNavSphereStartUpNorm;
    private float magnitudeNavSphereStart;
    private Quaternion quatNavSphereCenterStart;

    private int status = 0;
    private int countUpdates = 0;

    // Update is called once per frame
    void Update()
    {
        // az irányításhoz az kell, hogy a chaperone-hoz viszonyítva kapjam meg, így tudom csak jól számolni a gyorsulás értékeket, még ha körbenézek is
        if ( inputNavSphereLeftStart.state==true && inputNavSphereRightStart.state==true )
        {
            if ( this.status==0 )
                countUpdates = 0;
            else
                countUpdates++;

            Vector3 posNavSphereLeft = Player.instance.leftHand.transform.position - Player.instance.hmdTransform.position;
            Vector3 posNavSphereRight = Player.instance.rightHand.transform.position - Player.instance.hmdTransform.position;
            Vector3 posNavSphereCenter = ( posNavSphereLeft + posNavSphereRight )/2;
            Vector3 diffLeftToRight = posNavSphereRight - posNavSphereLeft;
            Vector3 diffLeftToRightNorm = diffLeftToRight.normalized;
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
                posNavSphereLeft.z = this.posNavSphereStartLeft.z + 0.9f;
                posNavSphereRight.z = this.posNavSphereStartRight.z - 0.9f;
            }
            if ( countUpdates>=2000 )
            {
                posNavSphereLeft = this.posNavSphereStartLeft;
                posNavSphereRight = this.posNavSphereStartRight;
            }
            posNavSphereCenter = ( posNavSphereLeft + posNavSphereRight )/2;
            diffLeftToRight = posNavSphereRight - posNavSphereLeft;
            diffLeftToRightNorm = diffLeftToRight.normalized;

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
                float deltaTime = Time.deltaTime;

                Vector3 diffCenter = posNavSphereCenter - this.posNavSphereCenterStart;
                // dot product kell ahhoz, hogy egy vektor adott irányú komponensét határozzam meg
                //Vector3 diffForward = this.dirNavSphereStartForwardNorm * Vector3.Dot( this.dirNavSphereStartForwardNorm,diffCenter );
                //Vector3 diffRight = this.dirNavSphereStartRightNorm * Vector3.Dot( this.dirNavSphereStartRightNorm,diffCenter );
                //Vector3 diffUp = this.dirNavSphereStartUpNorm * Vector3.Dot( this.dirNavSphereStartUpNorm,diffCenter );
                float velocityForward = Vector3.Dot( this.dirNavSphereStartForwardNorm,diffCenter );
                float velocityUp = Vector3.Dot( this.dirNavSphereStartUpNorm,diffCenter );
                float velocityRight = Vector3.Dot( this.dirNavSphereStartRightNorm,diffCenter );

                // 2 quaternion diff-jét kell kiszámolni, majd ezzel kell megszorozni a hmd rotation-jét
                // https://forum.unity.com/threads/get-the-difference-between-two-quaternions-and-add-it-to-another-quaternion.513187/
                Vector3 dirNavSphereForwardNorm = Vector3.Cross( diffLeftToRight,Vector3.up ).normalized;
                Vector3 dirNavSphereUpNorm = Vector3.Cross( dirNavSphereForwardNorm,diffLeftToRightNorm ).normalized;
                Quaternion quatNavSphere = Quaternion.LookRotation( dirNavSphereForwardNorm,dirNavSphereUpNorm ).normalized;
                //Quaternion quatTmp = this.quatNavSphereCenterStart * Quaternion.Inverse( quatNavSphere );

                //if ( countUpdates%100==0 ) Debug.Log( string.Format( "dirNavSphereUpNorm ({0},{1},{2})",dirNavSphereUpNorm.x,dirNavSphereUpNorm.y,dirNavSphereUpNorm.z ) );

                // ez is van: https://answers.unity.com/questions/35541/problem-finding-relative-rotation-from-one-quatern.html                
                Quaternion quatTmp1 = Quaternion.Inverse( this.quatNavSphereCenterStart ) * quatNavSphere;
                if ( countUpdates%50==0 ) Debug.Log( string.Format( "quatTmp1 ({0},{1},{2},{3})",quatTmp1.x,quatTmp1.y,quatTmp1.z,quatTmp1.w ) );

                Quaternion quatTmp2 = Quaternion.Lerp( Quaternion.identity,quatNavSphere,deltaTime/10f );
                if ( countUpdates%50==0 ) Debug.Log( string.Format( "quatTmp2 ({0},{1},{2},{3})",quatTmp2.x,quatTmp2.y,quatTmp2.z,quatTmp2.w ) );

                this.transform.rotation *= quatTmp2;
                if ( countUpdates%50==0 ) Debug.Log( string.Format( "rotation ({0},{1},{2},{3})",this.transform.rotation.x,this.transform.rotation.y,this.transform.rotation.z,this.transform.rotation.w ) );

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
    }
}
