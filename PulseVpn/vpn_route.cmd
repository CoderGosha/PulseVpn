echo ===Dialing=====================================================================

rasdial "engy"

echo ===Routing=====================================================================

route add 192.168.1.216 mask 255.255.255.255 192.168.3.145 METRIC 3
route add 192.168.1.226 mask 255.255.255.255 192.168.3.145 METRIC 3
route add 192.168.1.42 mask 255.255.255.255 192.168.3.145 METRIC 3
route add 192.168.1.49 mask 255.255.255.255 192.168.3.145 METRIC 3

