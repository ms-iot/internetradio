var express = require('express');
var router = express.Router();

function sendCommandToApp(command, res)
{
    var appServiceConnection = new Windows.ApplicationModel.AppService.AppServiceConnection();
    appServiceConnection.appServiceName = "InternetRadio.RadioManager";
    appServiceConnection.packageFamilyName = "InternetRadio-uwp_rh2vsgh482be2";
    appServiceConnection.openAsync().done(function (connectionState) {
        if (connectionState == 0) {
            var message = new Windows.Foundation.Collections.ValueSet();
            message.insert("Command", command);
            appServiceConnection.sendMessageAsync(message).done(
                function (appServiceResponse) {
                    res.send(appServiceResponse.message);
                    appServiceConnection.close();
                });
        }
        else {
            res.send("Failed to connect to App:" + connectionState);
        }
    });
}

/* GET home page. */
router.get('/', function (req, res) {
    res.render('index', { title: 'Express' });
});

router.get('/radio', function (req, res) {
    var radioCommand = req.query.command;
    
    sendCommandToApp(radioCommand, res);
   
});

router.get('/presets', function (req, res) {
    var radioCommand = req.query.command;
    var appServiceConnection = new Windows.ApplicationModel.AppService.AppServiceConnection();
    appServiceConnection.appServiceName = "InternetRadio.RadioManager";
    appServiceConnection.packageFamilyName = "InternetRadio-uwp_rh2vsgh482be2";
    appServiceConnection.openAsync().done(function (connectionState) {
        if (connectionState == 0) {
           // var message = new Windows.Foundation.Collections.ValueSet();
            //message.insert("Command", radioCommand);
            //appServiceConnection.sendMessageAsync(message).done(function () { });
        }
    });
    
    res.send(radioCommand);
});


module.exports = router;