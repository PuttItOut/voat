﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voat.Configuration;
using Voat.RulesEngine;
using Voat.Utilities;

namespace Voat.Rules.Posting
{
    //TODO: This rule need refactored, too many db calls.
    [RuleDiscovery("Approved if a user doesn't surpass Voat's submission throttling policy.", "approved = !(throttlePolicy.Surpassed = true)")]
    public class PostSubmissionRule : BaseSubverseBanRule
    {
        public PostSubmissionRule()
            : base("Submission Post Rule", "8.0", RuleScope.PostSubmission)
        {
        }

        protected override RuleOutcome EvaluateRule(VoatRuleContext context)
        {
            //this checks global ban
            var result = base.EvaluateRule(context);

            if (result.IsAllowed)
            {
                bool isMod = context.UserData.Information.Moderates.Any(x => x == context.Subverse.Name);

                // check posting quotas if user is posting to subs they do not moderate
                if (!isMod)
                {
                    // reject if user has reached global daily submission quota
                    if (UserHelper.UserDailyGlobalPostingQuotaUsed(context.UserName))
                    {
                        result = CreateOutcome(RuleResult.Denied, "You have reached your daily global submission quota");
                    }
                    // reject if user has reached global hourly submission quota
                    else if (UserHelper.UserHourlyGlobalPostingQuotaUsed(context.UserName))
                    {
                        result = CreateOutcome(RuleResult.Denied, "You have reached your hourly global submission quota");
                    }
                    // check if user has reached hourly posting quota for target subverse
                    else if (UserHelper.UserHourlyPostingQuotaForSubUsed(context.UserName, context.Subverse.Name))
                    {
                        result = CreateOutcome(RuleResult.Denied, "You have reached your hourly submission quota for this subverse");
                    }
                    // check if user has reached daily posting quota for target subverse
                    else if (UserHelper.UserDailyPostingQuotaForSubUsed(context.UserName, context.Subverse.Name))
                    {
                        result = CreateOutcome(RuleResult.Denied, "You have reached your daily submission quota for this subverse");
                    }
                    else if (context.Subverse.IsAuthorizedOnly)
                    {
                        result = CreateOutcome(RuleResult.Denied, "You are not authorized to submit links or start discussions in this subverse. Please contact subverse moderators for authorization.");
                    }
                }

                // if user CCP or SCP is less than -10, allow only X submissions per 24 hours
                if ((context.UserData.Information.CommentPoints.Sum <= -10 || context.UserData.Information.SubmissionPoints.Sum <= -10)
                    && UserHelper.UserDailyPostingQuotaForNegativeScoreUsed(context.UserName))
                {
                    result = CreateOutcome(RuleResult.Denied, String.Format("You have reached your daily submission quota. Your current quota is {0} submission(s) per 24 hours.", Settings.DailyPostingQuotaForNegativeScore));
                }
            }
            return result;
        }
    }
}
