using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class ScriptNavigationSphere : MonoBehaviour
{
    public SteamVR_Action_Boolean inputNavSphereLeftStart;
    public SteamVR_Action_Boolean inputNavSphereRightStart;

    // ezek az értékek a chaperone-hoz viszonyítottan kellenek
    private Vector3 posNavSphereCenterStart;
    private Vector3 dirNavSphereStartForwardNorm;
    private Vector3 dirNavSphereStartRightNorm;
    private Vector3 dirNavSphereStartUpNorm;
    private float magnitudeNavSphereStart;
    private Quaternion quatNavSphereCenterStart;

    private int status = 0;

    // Update is called once per frame
    void Update()
    {
        // az irányításhoz az kell, hogy a chaperone-hoz viszonyítva kapjam meg, így tudom csak jól számolni a gyorsulás értékeket, még ha körbenézek is
        if ( inputNavSphereLeftStart.state==true && inputNavSphereRightStart.state==true )
        {
            Vector3 posNavSphereLeft  = Player.instance.leftHand.transform.position - Player.instance.hmdTransform.position;
            Vector3 posNavSphereRight  = Player.instance.rightHand.transform.position - Player.instance.hmdTransform.position;
            Vector3 posNavSphereCenter = ( posNavSphereLeft + posNavSphereRight )/2;
            Vector3 diffLeftToRight = posNavSphereRight - posNavSphereLeft;
            Vector3 diffLeftToRightNorm = diffLeftToRight.normalized;
            if ( this.status==0 )
            {
                this.posNavSphereCenterStart = posNavSphereCenter;
                //quatNavSphereCenterStart = Player.instance.hmdTransform.rotation;
                // merőleges kell a navSphere két széle közti egyenesre és a felfele irányra
                // ezt vektoriális szorzással számítom ki: dirQuat
                // https://docs.unity3d.com/ScriptReference/Vector3.Cross.html
                // quaternion-t dirQuat-ból és 0 fokos szögből számolom
                this.magnitudeNavSphereStart = diffLeftToRight.magnitude;
                this.dirNavSphereStartForwardNorm = Vector3.Cross( diffLeftToRight,Vector3.up ).normalized;
                this.dirNavSphereStartRightNorm = diffLeftToRightNorm;
                this.dirNavSphereStartUpNorm = Vector3.Cross( this.dirNavSphereStartForwardNorm,this.dirNavSphereStartRightNorm ).normalized;
                this.quatNavSphereCenterStart =  Quaternion.LookRotation( this.dirNavSphereStartForwardNorm,this.dirNavSphereStartUpNorm ).normalized;
                this.status = 1;
            }
            else
            {
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
                Quaternion quatNavSphere = Quaternion.LookRotation( this.dirNavSphereStartForwardNorm,this.dirNavSphereStartUpNorm ).normalized;
                Quaternion quatTmp = this.quatNavSphereCenterStart * Quaternion.Inverse( quatNavSphere );
                
                this.transform.rotation *= quatTmp.normalized;

                Vector3 vecForward = this.transform.rotation * Vector3.forward;
                Vector3 vecUp = this.transform.rotation * Vector3.up;
                Vector3 vecRight = this.transform.rotation * Vector3.right;

                float deltaTime = Time.deltaTime;
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
