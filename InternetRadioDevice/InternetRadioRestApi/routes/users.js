var express = require('express');
var router = express.Router();

var uwp = require("uwp");
uwp.projectNamespace("Windows");

/* GET users listing. */
router.get('/', function (req, res) {
    var radioCommand = req.query.command;
    var appServiceConnection = new Windows.ApplicationModel.AppService.AppServiceConnection();
    appServiceConnection.appServiceName = "InternetRadio.RadioManager";
    appServiceConnection.packageFamilyName = "InternetRadio-uwp_rh2vsgh482be2";
    appServiceConnection.openAsync().done(function (connectionState) 
        {
        if (connectionState == 0) {
            var message = new Windows.Foundation.Collections.ValueSet();
            message.insert("Command", radioCommand);
            appServiceConnection.sendMessageAsync(message).done(function () { });
        }
        });

    res.send(radioCommand);
});

module.exports = router;