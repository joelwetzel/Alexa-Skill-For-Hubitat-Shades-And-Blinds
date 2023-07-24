# Alexa Skill For Hubitat Shades And Blinds

## NOTE: THIS IS NOW DEPRECATED AND SHOULD NOT BE USED ANYMORE.  HUBITAT HAS ADDED SHADES AND BLINDS TO THEIR OUT-OF-THE-BOX ALEXA INTEGRATION.  USE THAT INSTEAD.

An Alexa skill to interact with a Hubitat smarthome hub, and expose shades and blinds to Alexa.  (Because the native Hubitat Alexa skill does not yet support shades and blinds.)  It uses the Hubitat Maker API to interact with Hubitat.

This is really just temporary proof-of-concept code, and useful until Hubitat adds support for the WindowShade capability to their own Alexa skill.

This skill is based off the code Amazon provided at:  https://github.com/alexa/skill-sample-csharp-smarthome-switch

It uses the new Alexa shades and blinds support as discussed here:  https://alexa.uservoice.com/forums/913024-alexa-smart-home/suggestions/34277038-support-for-blinds-or-shades-in-smart-home-with



### Installation
To install it, you'll want to follow the instructions here:  https://github.com/alexa/skill-sample-csharp-smarthome-switch/blob/master/instructions/README.md

But use my code, instead of the version in Amazon's sample.

You'll also need to add in your own IDs from your own Hubitat hub's Maker API endpoints.  You can find them in the page for the Maker API app.
