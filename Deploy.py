import datetime
import shutil
import os
import sys

_current_time = datetime.datetime.now()
_file_time_stamp = f"{_current_time.year}-{_current_time.hour}-{_current_time.minute}-{_current_time.second}"
_file_name = f"deployment-{_file_time_stamp}"
_backup_file_name = f'backup-{_file_time_stamp}'


def run_deploy(source_path, target_path, backup_path):
    print(f"Running deploy for target path: {target_path}")

    try:
        # Stop Service
        print("Stopping service...")
        os.system("sc stop ProbablyFriends")

        # Create backup
        print("Creating backup...")
        shutil.make_archive(os.path.join(
            backup_path, _backup_file_name), 'zip', target_path)
        
        # Zip artifacts and copy to target
        print("Zipping artifacts and copying to target...")
        shutil.make_archive(
            os.path.join(target_path, _file_name), 'zip', source_path)

        # Unzip artifacts
        print("Unpacking artifacts...")
        shutil.unpack_archive(os.path.join(
            target_path, f"{_file_name}.zip"), target_path)

        # Delete archive
        print("Deleting archive...")
        os.remove(os.path.join(target_path, f"{_file_name}.zip"))

        # Start Service
        print("Starting Service...")
        os.system("sc start ProbablyFriends")

    except:
        roll_back(target_path, backup_path)

    print("Done!")


def roll_back(target_path, backup_path):
    print(f"An Exception occured. Rolling back deploy.")
    os.remove(target_path)
    os.write(target_path, 'x')
    shutil.unpack_archive(os.path.join(
        backup_path, f"{_backup_file_name}.zip"), target_path)


run_deploy(sys.argv[1], sys.argv[2], sys.argv[3])
