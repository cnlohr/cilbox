#!/bin/bash

# Add : to separate out apckages/subfolders.
PACKAGES=""
SUBFOLDERS="Packages/com.cnlohr.cilbox"

cd Basis

rm -rf generate_unitypackage
mkdir -p generate_unitypackage

echo $SUBFOLDERS | tr : '\n' | while read ddv; do
    # WOW! This actually works to list files with spaces!!! 
    find $ddv -type f -name "*.meta" -print0 | while read -d $'\0' -r FV ; do
        #printf 'File found: %s\n' "$FV"
        ASSET=${FV:0:${#FV} - 5}
        GUID=$(cat "$FV" | grep guid: | cut -d' ' -f2 | cut -b-32 )
        mkdir -p generate_unitypackage/$GUID
        if [[ -f "$ASSET" ]]; then
            #echo "$ASSET" TO generate_unitypackage/$GUID/asset
            #echo ASSET COPY cp "$ASSET" generate_unitypackage/$GUID/asset
            cp "$ASSET" generate_unitypackage/$GUID/asset
        fi
        cp "$FV" generate_unitypackage/$GUID/asset.meta
        #GPNAME=$(echo ${ddv:0:${#ddv} - 4} | cut -d/ -f3-)
        FONLY=$(echo $FV | rev | cut -d. -f2- | rev)
        echo "${FONLY}"
        echo "${FONLY}" > generate_unitypackage/$GUID/pathname
    done
done

echo "Now, exporting .tgz's"

if [[ ! -z $PACKAGES ]]; then
    echo $PACKAGES | tr : '\n' | while read ddv; do
        rm -rf tmp
        mkdir tmp
        tar xzf $ddv -C tmp/
        find tmp -type f -name "*.meta" -print0 | while read -d $'\0' -r f ; do
            ASSET=${f:0:${#f} - 5}
            GUID=$(cat "$f" | grep guid: | cut -d' ' -f2 | cut -b-32 )
            mkdir -p generate_unitypackage/$GUID
            if [[ -f "$ASSET" ]]; then
                cp "$ASSET" generate_unitypackage/$GUID/asset
            fi
            cp $f generate_unitypackage/$GUID/asset.meta
            GPNAME=$(echo "${ddv:0:${#ddv} - 4}" | cut -d/ -f1-)
            FONLY=$(echo "$f" | rev | cut -d. -f2- | rev | cut -d/ -f3-)
            echo "${GPNAME}/${FONLY}" "$GUID"
            echo "${GPNAME}/${FONLY}" > generate_unitypackage/$GUID/pathname
        done
    done
fi

echo "Done exporting .tgz's"

#If you wanted to append...
#cp BasisClient.unitypackage BasisClient.tar.gz
#rm -rf BasisClient.tar
#gunzip BasisClient.tar.gz
#tar r --file ../BasisClient.tar .

cd generate_unitypackage
tar czf ../com.cnlohr.cilbox.unitypackage .
cd ..

rm generate_unitypackage -rf

