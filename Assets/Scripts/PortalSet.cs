using UnityEngine;
using System.Collections;

/*
 * A set of portalObjects that will be linked together. Both portalObjects will be treated the same.
 */
public class PortalSet : MonoBehaviour {
    
    /* The two portals that are linked together */
    public PortalObjects EntrancePortal;
    public PortalObjects ExitPortal;

    /* The object used as a border for the portal mesh. Leave it empty for a default border */
    public GameObject portalBorder;
    

    void Update() {

        /* Create the meshes of the portals */
        CreateMesh();

        /* Create the borders around each linked portal */
        CreateBorder();
    }

    void CreateMesh() {
        /*
         * Create the mesh for the linked portals using the given portalMesh. If there is no portalMesh,
         * default to use a generated mesh with the given defaultMeshSize values
         */

        EntrancePortal.CreatePortalMesh();
        ExitPortal.CreatePortalMesh();
    }

    void CreateBorder() {
        /*
         * Create a border for each portal. They will have the same border to ensure consistensy between portals.
         * The border used will be the portalBorder object. If it is null, a default border will be used.
         */

        if(portalBorder) {
            EntrancePortal.SetBorder(portalBorder);
            ExitPortal.SetBorder(portalBorder);
        }
        else {
            Debug.Log("CREATING A DEFAULT BORDER");
        }
    }
}
