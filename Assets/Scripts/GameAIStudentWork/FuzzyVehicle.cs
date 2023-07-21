// Remove the line above if you are submitting to GradeScope for a grade. But leave it if you only want to check
// that your code compiles and the autograder can access your public methods.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using GameAI;

// All the Fuzz
using Tochas.FuzzyLogic;
using Tochas.FuzzyLogic.MembershipFunctions;
using Tochas.FuzzyLogic.Evaluators;
using Tochas.FuzzyLogic.Mergers;
using Tochas.FuzzyLogic.Defuzzers;
using Tochas.FuzzyLogic.Expressions;

namespace GameAICourse
{

    public class FuzzyVehicle : AIVehicle
    {

        // TODO create some Fuzzy Set enumeration types, and member variables for:
        // Fuzzy Sets (input and output), one or more Fuzzy Value Sets, and Fuzzy
        // Rule Sets for each output.
        // Also, create some methods to instantiate each of the member variables

        // Here are some basic examples to get you started
        enum FzOutputThrottle {Brake, Coast, Accelerate }
        enum FzOutputWheel { TurnLeft, Straight, TurnRight }

        enum FzInputSpeed { Slow, Medium, Fast }

        enum FzInputDir { Left, Straight, Right }

        FuzzySet<FzInputSpeed> fzSpeedSet;

        FuzzySet<FzInputDir> fzDirSet;

        FuzzySet<FzOutputThrottle> fzThrottleSet;
        FuzzyRuleSet<FzOutputThrottle> fzThrottleRuleSet;

        FuzzySet<FzOutputWheel> fzWheelSet;
        FuzzyRuleSet<FzOutputWheel> fzWheelRuleSet;

        FuzzyValueSet fzInputValueSet = new FuzzyValueSet();

        // These are used for debugging (see ApplyFuzzyRules() call
        // in Update()
        FuzzyValueSet mergedThrottle = new FuzzyValueSet();
        FuzzyValueSet mergedWheel = new FuzzyValueSet();



        private FuzzySet<FzInputSpeed> GetSpeedSet()
        {
            FuzzySet<FzInputSpeed> set = new FuzzySet<FzInputSpeed>();

            IMembershipFunction slowFx = new ShoulderMembershipFunction(-100f, new Coords(40f, 1f), new Coords(65f, 0f), 100f);
            IMembershipFunction mediumFx = new TriangularMembershipFunction(new Coords(40f, 0f), new Coords(65f, 1f), new Coords(80f, 0f));
            IMembershipFunction fastFx = new ShoulderMembershipFunction(-100f, new Coords(65f, 0f), new Coords(80f, 1f), 100f);

            set.Set(FzInputSpeed.Slow, slowFx);
            set.Set(FzInputSpeed.Medium, mediumFx);
            set.Set(FzInputSpeed.Fast, fastFx);

            return set;
        }

        private FuzzySet<FzInputDir> GetDirSet()
        {
            FuzzySet<FzInputDir> set = new FuzzySet<FzInputDir>();

            IMembershipFunction leftFx = new ShoulderMembershipFunction(-180f, new Coords(-45f, 1f), new Coords(0f, 0f), 180f);
            IMembershipFunction straightFx = new TriangularMembershipFunction(new Coords(-45f, 0f), new Coords(0f, 1f), new Coords(45f, 0f));
            IMembershipFunction rightFx = new ShoulderMembershipFunction(-180f, new Coords(0f, 0f), new Coords(45f, 1f), 180f);

            set.Set(FzInputDir.Left, leftFx);
            set.Set(FzInputDir.Straight, straightFx);
            set.Set(FzInputDir.Right, rightFx);

            return set;
        }

        private FuzzySet<FzOutputThrottle> GetThrottleSet()
        {

            FuzzySet<FzOutputThrottle> set = new FuzzySet<FzOutputThrottle>();

            IMembershipFunction brakeFx = new ShoulderMembershipFunction(-1f, new Coords(-0.5f, 1f), new Coords(0f, 0f), 1f);
            IMembershipFunction coastFx = new TriangularMembershipFunction(new Coords(-0.5f, 0f), new Coords(0f, 1f), new Coords(0.5f, 0f));
            IMembershipFunction accelerateFx = new ShoulderMembershipFunction(-1f, new Coords(0f, 0f), new Coords(0.5f, 1f), 1f);

            set.Set(new FuzzyVariable<FzOutputThrottle>(FzOutputThrottle.Brake, brakeFx));
            set.Set(new FuzzyVariable<FzOutputThrottle>(FzOutputThrottle.Coast, coastFx));
            set.Set(new FuzzyVariable<FzOutputThrottle>(FzOutputThrottle.Accelerate, accelerateFx));

            return set;
        }

        private FuzzySet<FzOutputWheel> GetWheelSet()
        {

            FuzzySet<FzOutputWheel> set = new FuzzySet<FzOutputWheel>();

            IMembershipFunction turnLeftFx = new ShoulderMembershipFunction(-1f, new Coords(-0.2f, 1f), new Coords(0f, 0f), 100f);
            IMembershipFunction straightFx = new TriangularMembershipFunction(new Coords(-0.2f, 0f), new Coords(0f, 1f), new Coords(0.2f, 0f));
            IMembershipFunction turnRightFx = new ShoulderMembershipFunction(-1f, new Coords(0f, 0f), new Coords(0.2f, 1f), 1f);

            set.Set(new FuzzyVariable<FzOutputWheel>(FzOutputWheel.TurnLeft, turnLeftFx));
            set.Set(new FuzzyVariable<FzOutputWheel>(FzOutputWheel.Straight, straightFx));
            set.Set(new FuzzyVariable<FzOutputWheel>(FzOutputWheel.TurnRight, turnRightFx));

            return set;
        }


        private FuzzyRule<FzOutputThrottle>[] GetThrottleRules()
        {

            FuzzyRule<FzOutputThrottle>[] rules =
            {
                // TODO: Add some rules. Here is an example
                // (Note: these aren't necessarily good rules)
                If(FzInputSpeed.Slow).Then(FzOutputThrottle.Accelerate),
                If(FzInputSpeed.Medium).Then(FzOutputThrottle.Coast),
                If(FzInputSpeed.Fast).Then(FzOutputThrottle.Brake),
                // More example syntax
                //If(And(FzInputSpeed.Fast, Not(FzFoo.Bar)).Then(FzOutputThrottle.Accelerate),
            };

            return rules;
        }

        private FuzzyRule<FzOutputWheel>[] GetWheelRules()
        {

            FuzzyRule<FzOutputWheel>[] rules =
            {
                If(FzInputDir.Left).Then(FzOutputWheel.TurnLeft),
                If(FzInputDir.Straight).Then(FzOutputWheel.Straight),
                If(FzInputDir.Right).Then(FzOutputWheel.TurnRight),
            };

            return rules;
        }

        private FuzzyRuleSet<FzOutputThrottle> GetThrottleRuleSet(FuzzySet<FzOutputThrottle> throttle)
        {
            var rules = this.GetThrottleRules();
            return new FuzzyRuleSet<FzOutputThrottle>(throttle, rules);
        }

        private FuzzyRuleSet<FzOutputWheel> GetWheelRuleSet(FuzzySet<FzOutputWheel> wheel)
        {
            var rules = this.GetWheelRules();
            return new FuzzyRuleSet<FzOutputWheel>(wheel, rules);
        }


        protected override void Awake()
        {
            base.Awake();

            StudentName = "Sebastian Brumm";

            // Only the AI can control. No humans allowed!
            IsPlayer = false;

        }

        protected override void Start()
        {
            base.Start();

            // TODO: You can initialize a bunch of Fuzzy stuff here
            fzSpeedSet = this.GetSpeedSet();

            fzDirSet = this.GetDirSet();

            fzThrottleSet = this.GetThrottleSet();
            fzThrottleRuleSet = this.GetThrottleRuleSet(fzThrottleSet);

            fzWheelSet = this.GetWheelSet();
            fzWheelRuleSet = this.GetWheelRuleSet(fzWheelSet);
        }

        System.Text.StringBuilder strBldr = new System.Text.StringBuilder();

        override protected void Update()
        {

            // TODO Do all your Fuzzy stuff here and then
            // pass your fuzzy rule sets to ApplyFuzzyRules()
            
            // Remove these once you get your fuzzy rules working.
            // You can leave one hardcoded while you work on the other.
            // Both steering and throttle must be implemented with variable
            // control and not fixed/hardcoded!

            //HardCodeSteering(0f);
            //HardCodeThrottle(1f);
            
            // Simple example of fuzzification of vehicle state
            // The Speed is fuzzified and stored in fzInputValueSet
            fzSpeedSet.Evaluate(Speed, fzInputValueSet);

            float angle = Vector3.SignedAngle(Velocity, pathTracker.pathCreator.path.GetDirectionAtDistance(40), Vector3.up);
            fzDirSet.Evaluate(angle, fzInputValueSet);

            // ApplyFuzzyRules evaluates your rules and assigns Thottle and Steering accordingly
            // Also, some intermediate values are passed back for debugging purposes
            // Throttle = someValue; //[-1f, 1f] -1 is full brake, 0 is neutral, 1 is full throttle
            // Steering = someValue; // [-1f, 1f] -1 if full left, 0 is neutral, 1 is full right

            ApplyFuzzyRules<FzOutputThrottle, FzOutputWheel>(
                fzThrottleRuleSet,
                fzWheelRuleSet,
                fzInputValueSet,
                // access to intermediate state for debugging
                out var throttleRuleOutput,
                out var wheelRuleOutput,
                ref mergedThrottle,
                ref mergedWheel
                );

            
            // Use vizText for debugging output
            // You might also use Debug.DrawLine() to draw vectors on Scene view
            //if (vizText != null)
            //{
            //    strBldr.Clear();

            //    strBldr.AppendLine($"Demo Output");
            //    strBldr.AppendLine($"Comment out before submission");

                // You will probably want to selectively enable/disable printing
                // of certain fuzzy states or rules

            //    AIVehicle.DiagnosticPrintFuzzyValueSet<FzInputSpeed>(fzInputValueSet, strBldr);
  
            //    AIVehicle.DiagnosticPrintRuleSet<FzOutputThrottle>(fzThrottleRuleSet, throttleRuleOutput, strBldr);
            //    AIVehicle.DiagnosticPrintRuleSet<FzOutputWheel>(fzWheelRuleSet, wheelRuleOutput, strBldr);

            //    vizText.text = strBldr.ToString();
            //}

            // recommend you keep the base Update call at the end, after all your FuzzyVehicle code so that
            // control inputs can be processed properly (e.g. Throttle, Steering)
            base.Update();
        }

    }
}
