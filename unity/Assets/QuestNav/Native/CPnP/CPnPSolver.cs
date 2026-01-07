using System;
using System.Collections.Generic;
using QuestNav.Native.AprilTag;

namespace QuestNav.Native.CPnP
{
    public class CPnPSolver
    {
        public CPnPResult CPnPSolve(AprilTagDetection[] detections)
        {
            var corners = new List<double>();
            
            foreach (var detection in detections)
            {
                corners.Add(detection.Corner0.x);
                corners.Add(detection.Corner0.y);
                
                corners.Add(detection.Corner1.x);
                corners.Add(detection.Corner1.y);
                
                corners.Add(detection.Corner2.x);
                corners.Add(detection.Corner2.y);
                
                corners.Add(detection.Corner3.x);
                corners.Add(detection.Corner3.y);
            }
            int error = CPnPNatives.cpnp_solve(corners.ToArray(),);

            if (error is not 0)
            {
                
            }
        }
    }
}
